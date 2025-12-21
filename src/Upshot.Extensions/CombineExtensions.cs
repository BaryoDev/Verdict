using System;
using System.Collections.Generic;
using System.Linq;

namespace Upshot.Extensions;

/// <summary>
/// Extension methods for combining and merging Result types.
/// </summary>
public static class CombineExtensions
{
    // ==================== Merge Operations ====================

    /// <summary>
    /// Merges multiple Result<T> into a single MultiResult<T>.
    /// If all succeed, returns success with the first value.
    /// If any fail, returns failure with all errors.
    /// </summary>
    public static MultiResult<T> Merge<T>(params Result<T>[] results)
    {
        if (results == null || results.Length == 0)
            throw new ArgumentException("At least one result is required", nameof(results));

        var errors = new List<Error>();
        T? firstValue = default;
        bool hasSuccess = false;

        foreach (var result in results)
        {
            if (result.IsSuccess && !hasSuccess)
            {
                firstValue = result.Value;
                hasSuccess = true;
            }
            else if (result.IsFailure)
            {
                errors.Add(result.Error);
            }
        }

        return errors.Count == 0
            ? MultiResult<T>.Success(firstValue!)
            : MultiResult<T>.Failure(ErrorCollection.Create(errors));
    }

    /// <summary>
    /// Merges multiple non-generic Results into a single MultiResult.
    /// </summary>
    public static MultiResult Merge(params Result[] results)
    {
        if (results == null || results.Length == 0)
            throw new ArgumentException("At least one result is required", nameof(results));

        var errors = results
            .Where(r => r.IsFailure)
            .Select(r => r.Error)
            .ToList();

        return errors.Count == 0
            ? MultiResult.Success()
            : MultiResult.Failure(ErrorCollection.Create(errors));
    }

    // ==================== Combine Operations ====================

    /// <summary>
    /// Combines two results into a tuple result.
    /// </summary>
    public static Result<(T1, T2)> Combine<T1, T2>(
        Result<T1> result1,
        Result<T2> result2)
    {
        if (result1.IsSuccess && result2.IsSuccess)
            return Result<(T1, T2)>.Success((result1.Value, result2.Value));

        if (result1.IsFailure)
            return Result<(T1, T2)>.Failure(result1.Error);

        return Result<(T1, T2)>.Failure(result2.Error);
    }

    /// <summary>
    /// Combines three results into a tuple result.
    /// </summary>
    public static Result<(T1, T2, T3)> Combine<T1, T2, T3>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3)
    {
        if (result1.IsSuccess && result2.IsSuccess && result3.IsSuccess)
            return Result<(T1, T2, T3)>.Success((result1.Value, result2.Value, result3.Value));

        if (result1.IsFailure)
            return Result<(T1, T2, T3)>.Failure(result1.Error);
        if (result2.IsFailure)
            return Result<(T1, T2, T3)>.Failure(result2.Error);

        return Result<(T1, T2, T3)>.Failure(result3.Error);
    }

    /// <summary>
    /// Combines four results into a tuple result.
    /// </summary>
    public static Result<(T1, T2, T3, T4)> Combine<T1, T2, T3, T4>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3,
        Result<T4> result4)
    {
        if (result1.IsSuccess && result2.IsSuccess && result3.IsSuccess && result4.IsSuccess)
            return Result<(T1, T2, T3, T4)>.Success((result1.Value, result2.Value, result3.Value, result4.Value));

        if (result1.IsFailure)
            return Result<(T1, T2, T3, T4)>.Failure(result1.Error);
        if (result2.IsFailure)
            return Result<(T1, T2, T3, T4)>.Failure(result2.Error);
        if (result3.IsFailure)
            return Result<(T1, T2, T3, T4)>.Failure(result3.Error);

        return Result<(T1, T2, T3, T4)>.Failure(result4.Error);
    }

    // ==================== CombineAll (Multi-Error) ====================

    /// <summary>
    /// Combines multiple results, collecting all errors if any fail.
    /// </summary>
    public static MultiResult<(T1, T2)> CombineAll<T1, T2>(
        Result<T1> result1,
        Result<T2> result2)
    {
        var errors = new List<Error>();

        if (result1.IsFailure) errors.Add(result1.Error);
        if (result2.IsFailure) errors.Add(result2.Error);

        return errors.Count == 0
            ? MultiResult<(T1, T2)>.Success((result1.Value, result2.Value))
            : MultiResult<(T1, T2)>.Failure(ErrorCollection.Create(errors));
    }

    /// <summary>
    /// Combines three results, collecting all errors if any fail.
    /// </summary>
    public static MultiResult<(T1, T2, T3)> CombineAll<T1, T2, T3>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3)
    {
        var errors = new List<Error>();

        if (result1.IsFailure) errors.Add(result1.Error);
        if (result2.IsFailure) errors.Add(result2.Error);
        if (result3.IsFailure) errors.Add(result3.Error);

        return errors.Count == 0
            ? MultiResult<(T1, T2, T3)>.Success((result1.Value, result2.Value, result3.Value))
            : MultiResult<(T1, T2, T3)>.Failure(ErrorCollection.Create(errors));
    }
}
