using System;
using System.Collections.Generic;

namespace Verdict;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// Implemented as a readonly struct so the success path allocates nothing.
/// <para>
/// Immutable once created and safe to read from multiple threads. Reassigning a
/// shared field concurrently is NOT safe: the struct exceeds pointer size, so a
/// reader can observe a partially written value. Guard mutable shared slots.
/// </para>
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T _value;
    private readonly Error _error;
    private readonly bool _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the result represents a success.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the result represents a failure.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// Gets the success value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
    public T Value
    {
        get
        {
            if (!_isSuccess)
            {
                throw new InvalidOperationException(
                    $"Cannot access Value on a failed result. Error: [{_error.Code}] {_error.Message}");
            }
            return _value;
        }
    }

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result.</exception>
    public Error Error
    {
        get
        {
            if (_isSuccess)
            {
                throw new InvalidOperationException("Cannot access Error on a successful result.");
            }

            // Validate we have a real error (not default struct initialization)
            if (string.IsNullOrEmpty(_error.Code) && string.IsNullOrEmpty(_error.Message))
            {
                throw new InvalidOperationException(
                    "Result is in invalid state (likely from default struct initialization). " +
                    "Always use Result<T>.Success() or Result<T>.Failure() to create results.");
            }

            return _error;
        }
    }

    /// <summary>
    /// Gets the value if successful, or default(T) if failed.
    /// </summary>
    public T? ValueOrDefault => _isSuccess ? _value : default;

    /// <summary>
    /// Gets the value if successful, or the specified fallback value if failed.
    /// </summary>
    public T ValueOr(T fallback) => _isSuccess ? _value : fallback;

    /// <summary>
    /// Gets the value if successful, or the result of the fallback factory if failed.
    /// </summary>
    public T ValueOr(Func<Error, T> fallbackFactory)
    {
        if (fallbackFactory == null) throw new ArgumentNullException(nameof(fallbackFactory));
        return _isSuccess ? _value : fallbackFactory(_error);
    }

    private Result(T value)
    {
        _value = value;
        _error = default;
        _isSuccess = true;
    }

    private Result(Error error)
    {
        // Safe to use default! because _isSuccess=false guarantees Value property will never be accessed
        // The Value getter throws InvalidOperationException when IsFailure is true
        _value = default!;
        _error = error;
        _isSuccess = false;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static Result<T> Failure(string code, string message) =>
        new(new Error(code, message));

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        _isSuccess
            ? $"Success({_value})"
            : $"Failure([{_error.Code}] {_error.Message})";

    /// <summary>
    /// Determines whether this result equals another.
    /// Successes compare by value, failures compare by error.
    /// </summary>
    public bool Equals(Result<T> other)
    {
        if (_isSuccess != other._isSuccess)
        {
            return false;
        }

        return _isSuccess
            ? EqualityComparer<T>.Default.Equals(_value, other._value)
            : _error.Equals(other._error);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <summary>
    /// Returns a hash code for this result.
    /// </summary>
    public override int GetHashCode()
    {
        // Implemented by hand rather than with System.HashCode, which is not
        // available on netstandard2.0 without an extra dependency.
        unchecked
        {
            var hash = _isSuccess ? 17 : 23;
            hash = (hash * 31) + (_isSuccess
                ? (_value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(_value))
                : _error.GetHashCode());
            return hash;
        }
    }

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    /// <summary>
    /// Deconstructs the result into its components for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out T? value, out Error error)
    {
        isSuccess = _isSuccess;
        value = _isSuccess ? _value : default;
        error = _isSuccess ? default : _error;
    }
}
