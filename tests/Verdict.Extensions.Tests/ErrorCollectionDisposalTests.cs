using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verdict.Extensions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// A disposed ErrorCollection returns its buffer to ArrayPool.Shared but the
/// struct keeps the reference. Reading it afterwards yields whatever the next
/// renter wrote, which across requests is another caller's data.
/// </summary>
public class ErrorCollectionDisposalTests
{
    /// <summary>
    /// Builds a collection large enough to actually be pooled.
    /// </summary>
    /// <remarks>
    /// Collections of <see cref="ErrorCollection.PoolingThreshold"/> errors or
    /// fewer are allocated outright and have no rental to return, so disposing
    /// one is a no-op and the tests below would pass for the wrong reason. The
    /// caller's errors stay at the front, so indexing and First are unaffected.
    /// </remarks>
    private static ErrorCollection Pooled(params Error[] errors)
    {
        var padded = new List<Error>(errors);
        while (padded.Count <= ErrorCollection.PoolingThreshold)
        {
            padded.Add(new Error("PAD", "padding to exceed the pooling threshold"));
        }

        return ErrorCollection.Create(padded);
    }

    [Fact]
    public void Indexer_AfterDispose_Throws()
    {
        var collection = Pooled(new Error("A", "one"));
        collection.Dispose();

        Assert.Throws<ObjectDisposedException>(() => collection[0]);
    }

    [Fact]
    public void AsSpan_AfterDispose_Throws()
    {
        var collection = Pooled(new Error("A", "one"));
        collection.Dispose();

        Assert.Throws<ObjectDisposedException>(() => collection.AsSpan().Length);
    }

    [Fact]
    public void First_AfterDispose_Throws()
    {
        var collection = Pooled(new Error("A", "one"));
        collection.Dispose();

        Assert.Throws<ObjectDisposedException>(() => collection.First());
    }

    [Fact]
    public void ToArray_AfterDispose_Throws()
    {
        var collection = Pooled(new Error("A", "one"));
        collection.Dispose();

        Assert.Throws<ObjectDisposedException>(() => collection.ToArray());
    }

    [Fact]
    public void Count_AfterDispose_Throws()
    {
        var collection = Pooled(new Error("A", "one"));
        collection.Dispose();

        Assert.Throws<ObjectDisposedException>(() => collection.Count);
    }

    [Fact]
    public void DisposedCollection_CannotObserveAnotherRentersData()
    {
        var collection = Pooled(new Error("TENANT_A", "confidential"));
        collection.Dispose();

        // Somebody else takes the buffer and writes to it.
        var stolen = ArrayPool<Error>.Shared.Rent(1);
        stolen[0] = new Error("TENANT_B", "other tenant");

        try
        {
            Assert.Throws<ObjectDisposedException>(() => collection[0]);
        }
        finally
        {
            ArrayPool<Error>.Shared.Return(stolen, clearArray: true);
        }
    }

    [Fact]
    public void DisposingACopy_InvalidatesTheOriginal()
    {
        // ErrorCollection is a struct, so a copy shares the underlying buffer.
        // The original must fail loudly rather than read pooled memory.
        var original = Pooled(new Error("A", "one"));

        static void DisposesItsCopy(ErrorCollection copy) => copy.Dispose();
        DisposesItsCopy(original);

        Assert.Throws<ObjectDisposedException>(() => original[0]);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var collection = Pooled(new Error("A", "one"));

        collection.Dispose();
        var second = Record.Exception(() => collection.Dispose());

        Assert.Null(second);
    }

    [Fact]
    public void Create_WhenCollectionEnumerationThrows_ReturnsClearedBufferToPool()
    {
        // Above PoolingThreshold, so this exercises the pooled path. Below it the
        // collection is allocated outright and nothing is rented, which would make
        // this test pass without ever touching the behaviour it guards.
        var pool = new TrackingArrayPool<Error>(16);
        var errors = new MisreportingCollection(
            reportedCount: 16,
            throwAfterFirst: true,
            new Error("SENSITIVE", "must be cleared"));

        var createWithPool = typeof(ErrorCollection).GetMethod(
            "CreateWithPool",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(createWithPool);
        var invocation = Assert.Throws<TargetInvocationException>(() =>
        {
            createWithPool.Invoke(null, new object[] { errors, pool });
        });
        Assert.IsType<InvalidOperationException>(invocation.InnerException);

        Assert.Equal(1, pool.RentCount);
        Assert.Equal(1, pool.ReturnCount);
        Assert.All(pool.Buffer, error => Assert.Equal(default, error));
    }

    [Fact]
    public void Create_WhenCollectionYieldsFewerItems_UsesActualCount()
    {
        var errors = new MisreportingCollection(
            reportedCount: 4,
            throwAfterFirst: false,
            new Error("A", "one"),
            new Error("B", "two"));

        using var collection = ErrorCollection.Create(errors);

        Assert.Equal(2, collection.Count);
        Assert.Equal(new[] { "A", "B" }, Array.ConvertAll(collection.ToArray(), error => error.Code));
    }

    [Fact]
    public void NonPooledCollection_RemainsUsableAfterDispose()
    {
        // Create(Error) and Create(params Error[]) allocate their own array and
        // never touch the pool, so disposal has nothing to invalidate.
        var single = ErrorCollection.Create(new Error("A", "one"));
        single.Dispose();

        Assert.Equal(1, single.Count);
        Assert.Equal("A", single[0].Code);
    }

    [Fact]
    public void DefaultCollection_IsSafeToUseAndDispose()
    {
        var empty = default(ErrorCollection);

        empty.Dispose();

        Assert.Equal(0, empty.Count);
        Assert.False(empty.HasErrors);
        Assert.Equal(0, empty.AsSpan().Length);
    }

    [Fact]
    public void UsingStatement_DisposesAndBlocksLaterReads()
    {
        ErrorCollection escaped;
        using (var scoped = Pooled(new Error("A", "one")))
        {
            Assert.Equal("A", scoped[0].Code);
            escaped = scoped;
        }

        Assert.Throws<ObjectDisposedException>(() => escaped[0]);
    }

    private sealed class MisreportingCollection : ICollection<Error>
    {
        private readonly Error[] _errors;
        private readonly bool _throwAfterFirst;

        internal MisreportingCollection(int reportedCount, bool throwAfterFirst, params Error[] errors)
        {
            Count = reportedCount;
            _throwAfterFirst = throwAfterFirst;
            _errors = errors;
        }

        public int Count { get; }
        public bool IsReadOnly => true;

        public IEnumerator<Error> GetEnumerator()
        {
            foreach (var error in _errors)
            {
                yield return error;
                if (_throwAfterFirst)
                {
                    throw new InvalidOperationException("enumeration failed");
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(Error item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(Error item) => Array.IndexOf(_errors, item) >= 0;
        public void CopyTo(Error[] array, int arrayIndex) => _errors.CopyTo(array, arrayIndex);
        public bool Remove(Error item) => throw new NotSupportedException();
    }

    private sealed class TrackingArrayPool<T> : ArrayPool<T>
    {
        internal TrackingArrayPool(int capacity)
        {
            Buffer = new T[capacity];
        }

        internal T[] Buffer { get; }
        internal int RentCount { get; private set; }
        internal int ReturnCount { get; private set; }

        public override T[] Rent(int minimumLength)
        {
            if (minimumLength > Buffer.Length || RentCount != ReturnCount)
                throw new InvalidOperationException("Tracking pool cannot satisfy the rental.");

            RentCount++;
            return Buffer;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (!ReferenceEquals(array, Buffer) || ReturnCount == RentCount)
                throw new InvalidOperationException("Tracking pool received an unknown buffer.");

            if (clearArray)
                Array.Clear(array, 0, array.Length);

            ReturnCount++;
        }
    }
}
