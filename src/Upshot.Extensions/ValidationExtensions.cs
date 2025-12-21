using System;
using System.Collections.Generic;
using System.Linq;

namespace Upshot.Extensions;

/// <summary>
/// Extension methods for fluent validation on Result types.
/// </summary>
public static class ValidationExtensions
{
    // ==================== Result<T> Validation ====================

    /// <summary>
    /// Ensures a condition is met, otherwise returns a failure.
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        if (result.IsFailure)
            return result;

        return predicate(result.Value)
            ? result
            : Result<T>.Failure(error);
    }

    /// <summary>
    /// Ensures a condition is met, otherwise returns a failure with the specified code and message.
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        string code,
        string message)
    {
        return result.Ensure(predicate, new Error(code, message));
    }

    /// <summary>
    /// Ensures multiple conditions are met, collecting all failures.
    /// Returns a MultiResult with all validation errors if any fail.
    /// </summary>
    public static MultiResult<T> EnsureAll<T>(
        this Result<T> result,
        params (Func<T, bool> predicate, Error error)[] validations)
    {
        if (validations == null || validations.Length == 0)
            return result.IsSuccess ? MultiResult<T>.Success(result.Value) : MultiResult<T>.Failure(result.Error);

        if (result.IsFailure)
            return MultiResult<T>.Failure(result.Error);

        var errors = new List<Error>();
        foreach (var (predicate, error) in validations)
        {
            if (!predicate(result.Value))
            {
                errors.Add(error);
            }
        }

        return errors.Count == 0
            ? MultiResult<T>.Success(result.Value)
            : MultiResult<T>.Failure(ErrorCollection.Create(errors));
    }

    /// <summary>
    /// Validates a value against multiple predicates, collecting all failures.
    /// </summary>
    public static MultiResult<T> ValidateAll<T>(
        T value,
        params (Func<T, bool> predicate, string code, string message)[] validations)
    {
        if (validations == null || validations.Length == 0)
            return MultiResult<T>.Success(value);

        var errors = new List<Error>();
        foreach (var (predicate, code, message) in validations)
        {
            if (!predicate(value))
            {
                errors.Add(new Error(code, message));
            }
        }

        return errors.Count == 0
            ? MultiResult<T>.Success(value)
            : MultiResult<T>.Failure(ErrorCollection.Create(errors));
    }

    // ==================== MultiResult<T> Validation ====================

    /// <summary>
    /// Ensures a condition is met on a MultiResult, adding to existing errors if it fails.
    /// </summary>
    public static MultiResult<T> Ensure<T>(
        this MultiResult<T> result,
        Func<T, bool> predicate,
        Error error)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        if (result.IsFailure)
            return result;

        return predicate(result.Value)
            ? result
            : MultiResult<T>.Failure(error);
    }

    /// <summary>
    /// Ensures a condition is met on a MultiResult.
    /// </summary>
    public static MultiResult<T> Ensure<T>(
        this MultiResult<T> result,
        Func<T, bool> predicate,
        string code,
        string message)
    {
        return result.Ensure(predicate, new Error(code, message));
    }
}
