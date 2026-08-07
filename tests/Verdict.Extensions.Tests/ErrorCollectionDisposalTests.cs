using System;
using System.Buffers;
using System.Collections.Generic;
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
    private static ErrorCollection Pooled(params Error[] errors) =>
        ErrorCollection.Create(new List<Error>(errors));

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
}
