using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace Verdict.Extensions;

/// <summary>
/// A struct-based collection for storing multiple errors with minimal allocation.
/// Uses ArrayPool for efficient memory management.
/// </summary>
public readonly struct ErrorCollection : IDisposable
{
    private readonly Error[] _errors;
    private readonly int _count;
    private readonly RentalTracker? _rentalTracker;

    /// <summary>
    /// Gets the number of errors in the collection.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The collection has been disposed.</exception>
    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _count;
        }
    }

    /// <summary>
    /// Gets whether this collection has any errors.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The collection has been disposed.</exception>
    public bool HasErrors
    {
        get
        {
            ThrowIfDisposed();
            return _count > 0;
        }
    }

    /// <summary>
    /// Gets a read-only span of the errors.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The collection has been disposed.</exception>
    public ReadOnlySpan<Error> AsSpan()
    {
        ThrowIfDisposed();
        return _errors.AsSpan(0, _count);
    }

    /// <summary>
    /// Gets whether this collection's pooled buffer has been returned.
    /// Always false for collections that do not use the pool.
    /// </summary>
    public bool IsDisposed => _rentalTracker?.IsDisposed ?? false;

    /// <summary>
    /// Disposal returns the buffer to ArrayPool.Shared, where another caller can
    /// rent it and overwrite the contents. Reading afterwards would surface that
    /// caller's data, so every accessor fails loudly instead.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_rentalTracker is { IsDisposed: true })
        {
            throw new ObjectDisposedException(nameof(ErrorCollection),
                "This ErrorCollection was disposed and its buffer returned to the pool. " +
                "Note that ErrorCollection is a struct: disposing any copy invalidates them all.");
        }
    }

    /// <summary>
    /// The size above which a collection is allocated outright rather than
    /// rented, so nothing oversized is handed to the shared pool.
    /// </summary>
    /// <remarks>
    /// <c>ArrayPool&lt;Error&gt;.Shared</c> keeps what it is given back. An
    /// <see cref="Error"/> is 24 bytes, so past roughly 3,500 of them the rented
    /// array is on the large object heap and the pool then holds it for the life
    /// of the process. A validation loop producing one error per field, against a
    /// large request, was enough to park several megabytes there.
    /// <para>
    /// The fix is to stop pooling, not to stop collecting: every error is still
    /// kept. 1,024 errors is 24 KB, comfortably under the 85 KB threshold.
    /// </para>
    /// </remarks>
    public const int PoolingCeiling = 1024;

    /// <summary>
    /// The size at or below which a collection is allocated outright instead of
    /// rented from the pool.
    /// </summary>
    /// <remarks>
    /// Measured on eight threads: pooled and disposed correctly costs 88 bytes an
    /// operation, an ordinary array costs 96, and pooled with the dispose
    /// forgotten costs 496 with six times the gen0 collections. The pool bought
    /// eight bytes when the caller got it right and cost four hundred when they
    /// did not, and forgetting is the default outcome for a struct that cannot be
    /// used with <c>using</c>. Below this size the pool is not worth its contract.
    /// <para>
    /// A collection built this way has no rental to return, so
    /// <see cref="Dispose"/> is a no-op on it and a copy handed out by a
    /// combinator cannot be invalidated by a sibling disposing.
    /// </para>
    /// </remarks>
    public const int PoolingThreshold = 8;

    private ErrorCollection(Error[] errors, int count, RentalTracker? rentalTracker)
    {
        _errors = errors;
        _count = count;
        _rentalTracker = rentalTracker;
    }

    /// <summary>
    /// Creates an error collection from a single error.
    /// </summary>
    public static ErrorCollection Create(Error error)
    {
        var array = new Error[1];
        array[0] = error;
        return new ErrorCollection(array, 1, null);
    }

    /// <summary>
    /// Creates an error collection from multiple errors.
    /// </summary>
    public static ErrorCollection Create(params Error[] errors)
    {
        if (errors == null || errors.Length == 0)
            return default;

        var array = new Error[errors.Length];
        Array.Copy(errors, array, errors.Length);
        return new ErrorCollection(array, errors.Length, null);
    }

    /// <summary>
    /// Creates an error collection from an enumerable of errors.
    /// Uses array pooling for better performance.
    /// </summary>
    public static ErrorCollection Create(IEnumerable<Error> errors) =>
        CreateWithPool(errors, ArrayPool<Error>.Shared);

    private static ErrorCollection CreateWithPool(IEnumerable<Error> errors, ArrayPool<Error> pool)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));
        if (pool == null)
            throw new ArgumentNullException(nameof(pool));

        // Fast path: if it's already an array, use the array overload
        if (errors is Error[] errorArray)
            return Create(errorArray);

        // Fast path: if it's a collection, we can get the count without enumerating
        if (errors is ICollection<Error> collection)
        {
            var expectedCount = collection.Count;
            if (expectedCount == 0)
                return default;

            // Outside the band the pool is worth using, allocate exactly: below it
            // the disposal contract costs more than the pool saves, above it the
            // pool would retain a large object heap array for the process lifetime.
            if (expectedCount <= PoolingThreshold || expectedCount > PoolingCeiling)
            {
                var exact = new Error[expectedCount];
                var filled = Fill(collection, exact, expectedCount);

                return filled == 0 ? default : new ErrorCollection(exact, filled, null);
            }

            var array = pool.Rent(expectedCount);
            try
            {
                var actualCount = Fill(collection, array, expectedCount);

                if (actualCount == 0)
                {
                    pool.Return(array, clearArray: true);
                    return default;
                }

                return new ErrorCollection(array, actualCount, new RentalTracker(array, pool));
            }
            catch
            {
                pool.Return(array, clearArray: true);
                throw;
            }
        }

        // Slow path: unknown size, must enumerate
        // Use a small initial buffer and grow if needed
        const int initialCapacity = 4;
        var buffer = pool.Rent(initialCapacity);
        var pooled = true;
        int count = 0;

        try
        {
            foreach (var error in errors)
            {
                if (count == buffer.Length)
                {
                    // Past the ceiling, grow on the heap and stop renting, so an
                    // unbounded input cannot park a large object heap array in the
                    // shared pool. Nothing is dropped either way: the buffer keeps
                    // growing, it just stops being the pool's problem.
                    var grownLength = buffer.Length * 2;
                    var stillPooled = pooled && grownLength <= PoolingCeiling;
                    var grown = stillPooled ? pool.Rent(grownLength) : new Error[grownLength];

                    // Copy before returning. Return clears the array, so reading
                    // from it afterwards yields nothing but zeroed entries.
                    Array.Copy(buffer, grown, count);

                    if (pooled)
                    {
                        pool.Return(buffer, clearArray: true);
                    }

                    buffer = grown;
                    pooled = stillPooled;
                }
                buffer[count++] = error;
            }

            if (count == 0)
            {
                if (pooled) pool.Return(buffer, clearArray: true);
                return default;
            }

            // The size was unknown going in. Now that it is known, a small result
            // gets the same no-contract treatment as the counted path above.
            if (pooled && count <= PoolingThreshold)
            {
                var exact = new Error[count];
                Array.Copy(buffer, exact, count);
                pool.Return(buffer, clearArray: true);
                return new ErrorCollection(exact, count, null);
            }

            return pooled
                ? new ErrorCollection(buffer, count, new RentalTracker(buffer, pool))
                : new ErrorCollection(buffer, count, null);
        }
        catch
        {
            if (pooled) pool.Return(buffer, clearArray: true);
            throw;
        }
    }

    /// <summary>
    /// Copies at most <paramref name="limit" /> errors into <paramref name="destination" />.
    /// </summary>
    /// <remarks>
    /// Indexes an <see cref="IList{T}" /> rather than enumerating it, because a
    /// <c>foreach</c> over an interface boxes the struct enumerator and that
    /// allocation showed up as 56 bytes a call, more than the array itself.
    /// Falls back to enumerating anything that is not a list.
    /// <para>
    /// The count is taken from the collection and a collection is free to lie
    /// about it, so this trusts the copy rather than the count and returns how
    /// many it actually wrote.
    /// </para>
    /// </remarks>
    private static int Fill(ICollection<Error> source, Error[] destination, int limit)
    {
        var filled = 0;

        if (source is IList<Error> list)
        {
            var take = list.Count < limit ? list.Count : limit;
            for (var i = 0; i < take; i++)
            {
                destination[filled++] = list[i];
            }

            return filled;
        }

        foreach (var error in source)
        {
            if (filled == limit)
            {
                break;
            }

            destination[filled++] = error;
        }

        return filled;
    }

    /// <summary>
    /// Gets the error at the specified index.
    /// </summary>
    public Error this[int index]
    {
        get
        {
            ThrowIfDisposed();
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range. Valid range: 0 to {_count - 1}");
            return _errors[index];
        }
    }

    /// <summary>
    /// Gets the first error in the collection.
    /// </summary>
    public Error First()
    {
        ThrowIfDisposed();
        if (_count == 0)
            throw new InvalidOperationException("Error collection is empty");
        return _errors[0];
    }

    /// <summary>
    /// Returns a collection that owns its own storage, so disposing this one
    /// cannot invalidate it.
    /// </summary>
    /// <remarks>
    /// A combinator hands out a second result carrying the first one's errors.
    /// Passing the collection straight through made the two share a rented array,
    /// so <c>a.DisposeErrors()</c> left <c>b.ErrorCount</c> throwing, and the user
    /// who followed the guidance to dispose got a broken object they never copied
    /// by hand.
    /// <para>
    /// Copies only when there is something to alias. A collection that was never
    /// pooled has no rental to return, so disposing it is already a no-op and it
    /// is returned unchanged. That covers everything up to
    /// <see cref="PoolingThreshold" /> errors, which is nearly every failure.
    /// </para>
    /// </remarks>
    public ErrorCollection Detach()
    {
        if (_rentalTracker is null)
        {
            return this;
        }

        ThrowIfDisposed();

        if (_count == 0)
        {
            return default;
        }

        var copy = new Error[_count];
        Array.Copy(_errors, copy, _count);
        return new ErrorCollection(copy, _count, null);
    }

    /// <summary>
    /// Converts the collection to an array.
    /// </summary>
    public Error[] ToArray()
    {
        ThrowIfDisposed();
        if (_count == 0)
            return Array.Empty<Error>();

        var result = new Error[_count];
        Array.Copy(_errors, result, _count);
        return result;
    }

    /// <summary>
    /// Returns the rented array to the pool if applicable.
    /// Thread-safe and idempotent: only the first call returns the array to the pool.
    /// Uses clearArray: true to prevent retaining Exception references in pooled arrays.
    /// </summary>
    public void Dispose()
    {
        _rentalTracker?.Return();
    }

    /// <summary>
    /// Returns a string representation of the error collection.
    /// </summary>
    public override string ToString() =>
        IsDisposed ? "ErrorCollection (disposed)"
        : _count == 0 ? "No errors"
        : $"{_count} error(s)";

    /// <summary>
    /// Tracks the rental state of a pooled array to ensure idempotent disposal.
    /// This is a class (reference type) so that all struct copies of ErrorCollection
    /// share the same tracker instance, preventing double-return to the pool.
    /// </summary>
    internal sealed class RentalTracker
    {
        private Error[]? _array;
        private readonly ArrayPool<Error> _pool;

        internal RentalTracker(Error[] array, ArrayPool<Error> pool)
        {
            _array = array;
            _pool = pool;
        }

        internal bool IsDisposed => Volatile.Read(ref _array) is null;

        internal void Return()
        {
            var array = Interlocked.Exchange(ref _array, null);
            if (array != null)
            {
                _pool.Return(array, clearArray: true);
            }
        }
    }
}
