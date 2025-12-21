using System;

namespace Upshot.Extensions;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with multiple errors.
/// Implemented as a readonly struct for performance with support for multiple errors.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct MultiResult<T> : IDisposable
{
    private readonly T _value;
    private readonly ErrorCollection _errors;
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
                    $"Cannot access Value on a failed result. {_errors.Count} error(s) occurred.");
            }
            return _value;
        }
    }

    /// <summary>
    /// Gets the errors.
    /// </summary>
    public ReadOnlySpan<Error> Errors => _errors.AsSpan();

    /// <summary>
    /// Gets the number of errors.
    /// </summary>
    public int ErrorCount => _errors.Count;

    /// <summary>
    /// Gets the value if successful, or default(T) if failed.
    /// </summary>
    public T? ValueOrDefault => _isSuccess ? _value : default;

    private MultiResult(T value)
    {
        _value = value;
        _errors = default;
        _isSuccess = true;
    }

    private MultiResult(ErrorCollection errors)
    {
        _value = default!;
        _errors = errors;
        _isSuccess = false;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static MultiResult<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    public static MultiResult<T> Failure(Error error) =>
        new(ErrorCollection.Create(error));

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    public static MultiResult<T> Failure(params Error[] errors) =>
        new(ErrorCollection.Create(errors));

    /// <summary>
    /// Creates a failed result with an error collection.
    /// </summary>
    public static MultiResult<T> Failure(ErrorCollection errors) =>
        new(errors);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static MultiResult<T> Failure(string code, string message) =>
        new(ErrorCollection.Create(new Error(code, message)));

    /// <summary>
    /// Converts this multi-result to a single-error Result, taking the first error.
    /// </summary>
    public Result<T> ToSingleResult()
    {
        return _isSuccess
            ? Result<T>.Success(_value)
            : Result<T>.Failure(_errors.First());
    }

    /// <summary>
    /// Implicitly converts a value to a successful multi-result.
    /// </summary>
    public static implicit operator MultiResult<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed multi-result.
    /// </summary>
    public static implicit operator MultiResult<T>(Error error) => Failure(error);

    /// <summary>
    /// Implicitly converts a Result to a MultiResult.
    /// </summary>
    public static implicit operator MultiResult<T>(Result<T> result)
    {
        return result.IsSuccess
            ? Success(result.Value)
            : Failure(result.Error);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        _isSuccess
            ? $"Success({_value})"
            : $"Failure({_errors.Count} error(s))";

    /// <summary>
    /// Deconstructs the result into its components for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out T? value, out ReadOnlySpan<Error> errors)
    {
        isSuccess = _isSuccess;
        value = _isSuccess ? _value : default;
        errors = _isSuccess ? ReadOnlySpan<Error>.Empty : _errors.AsSpan();
    }

    /// <summary>
    /// Disposes the error collection if needed.
    /// </summary>
    public void Dispose()
    {
        if (!_isSuccess)
        {
            _errors.Dispose();
        }
    }
}

/// <summary>
/// Represents the result of an operation with no return value that can either succeed or fail with multiple errors.
/// </summary>
public readonly struct MultiResult : IDisposable
{
    private readonly ErrorCollection _errors;
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
    /// Gets the errors.
    /// </summary>
    public ReadOnlySpan<Error> Errors => _errors.AsSpan();

    /// <summary>
    /// Gets the number of errors.
    /// </summary>
    public int ErrorCount => _errors.Count;

    private MultiResult(bool isSuccess)
    {
        _errors = default;
        _isSuccess = isSuccess;
    }

    private MultiResult(ErrorCollection errors)
    {
        _errors = errors;
        _isSuccess = false;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static MultiResult Success() => new(true);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    public static MultiResult Failure(Error error) =>
        new(ErrorCollection.Create(error));

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    public static MultiResult Failure(params Error[] errors) =>
        new(ErrorCollection.Create(errors));

    /// <summary>
    /// Creates a failed result with an error collection.
    /// </summary>
    public static MultiResult Failure(ErrorCollection errors) =>
        new(errors);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static MultiResult Failure(string code, string message) =>
        new(ErrorCollection.Create(new Error(code, message)));

    /// <summary>
    /// Converts this multi-result to a single-error Result, taking the first error.
    /// </summary>
    public Result ToSingleResult()
    {
        return _isSuccess
            ? Result.Success()
            : Result.Failure(_errors.First());
    }

    /// <summary>
    /// Implicitly converts an error to a failed multi-result.
    /// </summary>
    public static implicit operator MultiResult(Error error) => Failure(error);

    /// <summary>
    /// Implicitly converts a Result to a MultiResult.
    /// </summary>
    public static implicit operator MultiResult(Result result)
    {
        return result.IsSuccess
            ? Success()
            : Failure(result.Error);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        _isSuccess
            ? "Success"
            : $"Failure({_errors.Count} error(s))";

    /// <summary>
    /// Deconstructs the result into its components for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out ReadOnlySpan<Error> errors)
    {
        isSuccess = _isSuccess;
        errors = _isSuccess ? ReadOnlySpan<Error>.Empty : _errors.AsSpan();
    }

    /// <summary>
    /// Disposes the error collection if needed.
    /// </summary>
    public void Dispose()
    {
        if (!_isSuccess)
        {
            _errors.Dispose();
        }
    }
}
