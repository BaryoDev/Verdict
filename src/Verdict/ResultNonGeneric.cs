using System;

namespace Verdict;

/// <summary>
/// Represents the result of an operation with no return value that can either succeed or fail with an error.
/// Implemented as a readonly struct so the success path allocates nothing.
/// <para>
/// Immutable once created and safe to read from multiple threads. Reassigning a
/// shared field concurrently is NOT safe: the struct exceeds pointer size, so a
/// reader can observe a partially written value. Guard mutable shared slots.
/// </para>
/// </summary>
public readonly struct Result : IEquatable<Result>
{
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
                    "Always use Result.Success() or Result.Failure() to create results.");
            }

            return _error;
        }
    }

    private Result(bool isSuccess)
    {
        _error = default;
        _isSuccess = isSuccess;
    }

    private Result(Error error)
    {
        _error = error;
        _isSuccess = false;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static Result Failure(string code, string message) =>
        new(new Error(code, message));

    /// <summary>
    /// Creates a failed result with the specified error code, message, and exception.
    /// </summary>
    public static Result Failure(string code, string message, Exception exception) =>
        new(new Error(code, message, exception));

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Converts this non-generic result to a generic result with Unit value.
    /// </summary>
    public Result<Unit> ToGeneric() =>
        _isSuccess
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure(_error);

    /// <summary>
    /// Determines whether this result equals another.
    /// </summary>
    public bool Equals(Result other) =>
        _isSuccess == other._isSuccess && (_isSuccess || _error.Equals(other._error));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result other && Equals(other);

    /// <summary>
    /// Returns a hash code for this result.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return _isSuccess ? 17 : (23 * 31) + _error.GetHashCode();
        }
    }

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        _isSuccess
            ? "Success"
            : $"Failure([{_error.Code}] {_error.Message})";

    /// <summary>
    /// Deconstructs the result into its components for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out Error error)
    {
        isSuccess = _isSuccess;
        error = _isSuccess ? default : _error;
    }
}
