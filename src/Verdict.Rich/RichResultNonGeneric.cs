using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Verdict.Rich;

/// <summary>
/// Represents a non-generic Result with embedded rich metadata (success messages and error metadata).
/// This is an immutable struct where metadata is stored directly within the struct itself,
/// eliminating the need for external storage and preventing memory leaks.
/// </summary>
public readonly struct RichResult
{
    private readonly Result _result;
    private readonly ImmutableList<SuccessInfo>? _successes;
    private readonly ImmutableDictionary<string, object>? _errorMetadata;

    /// <summary>
    /// Gets a value indicating whether the result represents a success.
    /// </summary>
    public bool IsSuccess => _result.IsSuccess;

    /// <summary>
    /// Gets a value indicating whether the result represents a failure.
    /// </summary>
    public bool IsFailure => _result.IsFailure;

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result.</exception>
    public Error Error => _result.Error;

    /// <summary>
    /// Gets all success messages attached to this result.
    /// </summary>
    public IReadOnlyList<SuccessInfo> Successes => 
        _successes ?? (IReadOnlyList<SuccessInfo>)ImmutableList<SuccessInfo>.Empty;

    /// <summary>
    /// Gets all error metadata attached to this result.
    /// </summary>
    public IReadOnlyDictionary<string, object> ErrorMetadata => 
        _errorMetadata ?? (IReadOnlyDictionary<string, object>)ImmutableDictionary<string, object>.Empty;

    // Private constructor
    private RichResult(
        Result result,
        ImmutableList<SuccessInfo>? successes = null,
        ImmutableDictionary<string, object>? errorMetadata = null)
    {
        _result = result;
        _successes = successes;
        _errorMetadata = errorMetadata;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static RichResult Success() => 
        new(Result.Success());

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static RichResult Failure(Error error) => 
        new(Result.Failure(error));

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static RichResult Failure(string code, string message) => 
        new(Result.Failure(code, message));

    /// <summary>
    /// Adds a success message to the result.
    /// Returns a new RichResult with the message added.
    /// </summary>
    public RichResult WithSuccess(string message)
    {
        if (!IsSuccess) return this;
        if (message == null) throw new ArgumentNullException(nameof(message));

        var newSuccesses = _successes == null
            ? ImmutableList.Create(new SuccessInfo(message))
            : _successes.Add(new SuccessInfo(message));

        return new RichResult(_result, newSuccesses, _errorMetadata);
    }

    /// <summary>
    /// Adds a success info with metadata to the result.
    /// Returns a new RichResult with the success info added.
    /// </summary>
    public RichResult WithSuccess(SuccessInfo success)
    {
        if (!IsSuccess) return this;

        var newSuccesses = _successes == null
            ? ImmutableList.Create(success)
            : _successes.Add(success);

        return new RichResult(_result, newSuccesses, _errorMetadata);
    }

    /// <summary>
    /// Adds metadata to the error.
    /// Returns a new RichResult with the metadata added.
    /// </summary>
    public RichResult WithErrorMetadata(string key, object value)
    {
        if (IsSuccess) return this;
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        var newMetadata = _errorMetadata == null
            ? ImmutableDictionary.Create<string, object>().Add(key, value)
            : _errorMetadata.SetItem(key, value);

        return new RichResult(_result, _successes, newMetadata);
    }

    /// <summary>
    /// Implicitly converts a Result to a RichResult.
    /// </summary>
    public static implicit operator RichResult(Result result) => new(result);

    /// <summary>
    /// Implicitly converts a RichResult to a Result (metadata is lost).
    /// </summary>
    public static implicit operator Result(RichResult richResult) => richResult._result;

    /// <summary>
    /// Implicitly converts an error to a failed RichResult.
    /// </summary>
    public static implicit operator RichResult(Error error) => Failure(error);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString()
    {
        var baseStr = _result.ToString();
        
        if (IsSuccess && _successes != null && _successes.Count > 0)
        {
            return $"{baseStr} [+{_successes.Count} success messages]";
        }
        
        if (IsFailure && _errorMetadata != null && _errorMetadata.Count > 0)
        {
            return $"{baseStr} [+{_errorMetadata.Count} metadata items]";
        }
        
        return baseStr;
    }

    /// <summary>
    /// Deconstructs the result into its components for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out Error error)
    {
        isSuccess = IsSuccess;
        error = IsSuccess ? default : Error;
    }
}
