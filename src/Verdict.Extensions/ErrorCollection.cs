using System;
using System.Buffers;
using System.Collections.Generic;

namespace Verdict.Extensions;

/// <summary>
/// A struct-based collection for storing multiple errors with minimal allocation.
/// Uses ArrayPool for efficient memory management.
/// </summary>
public readonly struct ErrorCollection : IDisposable
{
    private readonly Error[] _errors;
    private readonly int _count;
    private readonly bool _isRented;

    /// <summary>
    /// Gets the number of errors in the collection.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets whether this collection has any errors.
    /// </summary>
    public bool HasErrors => _count > 0;

    /// <summary>
    /// Gets a read-only span of the errors.
    /// </summary>
    public ReadOnlySpan<Error> AsSpan() => _errors.AsSpan(0, _count);

    private ErrorCollection(Error[] errors, int count, bool isRented)
    {
        _errors = errors;
        _count = count;
        _isRented = isRented;
    }

    /// <summary>
    /// Creates an error collection from a single error.
    /// </summary>
    public static ErrorCollection Create(Error error)
    {
        var array = new Error[1];
        array[0] = error;
        return new ErrorCollection(array, 1, false);
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
        return new ErrorCollection(array, errors.Length, false);
    }

    /// <summary>
    /// Creates an error collection from an enumerable of errors.
    /// Uses array pooling for better performance.
    /// </summary>
    public static ErrorCollection Create(IEnumerable<Error> errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        // Fast path: if it's already an array, use the array overload
        if (errors is Error[] errorArray)
            return Create(errorArray);

        // Fast path: if it's a collection, we can get the count without enumerating
        if (errors is ICollection<Error> collection)
        {
            if (collection.Count == 0)
                return default;

            var array = ArrayPool<Error>.Shared.Rent(collection.Count);
            int i = 0;
            foreach (var error in collection)
            {
                array[i++] = error;
            }
            return new ErrorCollection(array, collection.Count, true);
        }

        // Slow path: unknown size, must enumerate
        // Use a small initial buffer and grow if needed
        const int initialCapacity = 4;
        var buffer = ArrayPool<Error>.Shared.Rent(initialCapacity);
        int count = 0;

        try
        {
            foreach (var error in errors)
            {
                if (count == buffer.Length)
                {
                    // Grow the buffer
                    var newBuffer = ArrayPool<Error>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, newBuffer, count);
                    ArrayPool<Error>.Shared.Return(buffer);
                    buffer = newBuffer;
                }
                buffer[count++] = error;
            }

            if (count == 0)
            {
                ArrayPool<Error>.Shared.Return(buffer);
                return default;
            }

            return new ErrorCollection(buffer, count, true);
        }
        catch
        {
            ArrayPool<Error>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// Gets the error at the specified index.
    /// </summary>
    public Error this[int index]
    {
        get
        {
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
        if (_count == 0)
            throw new InvalidOperationException("Error collection is empty");
        return _errors[0];
    }

    /// <summary>
    /// Converts the collection to an array.
    /// </summary>
    public Error[] ToArray()
    {
        if (_count == 0)
            return Array.Empty<Error>();

        var result = new Error[_count];
        Array.Copy(_errors, result, _count);
        return result;
    }

    /// <summary>
    /// Returns the rented array to the pool if applicable.
    /// IMPORTANT: Arrays are returned with clearArray: false to prevent data corruption
    /// when struct copies exist. Error structs are value types and safe to leave in pool.
    /// </summary>
    public void Dispose()
    {
        if (_isRented && _errors != null)
        {
            ArrayPool<Error>.Shared.Return(_errors, clearArray: false);
        }
    }

    /// <summary>
    /// Returns a string representation of the error collection.
    /// </summary>
    public override string ToString() =>
        _count == 0 ? "No errors" : $"{_count} error(s)";
}
