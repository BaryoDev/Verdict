using System;

namespace Verdict.Extensions;

/// <summary>
/// Extension methods for combining and merging Result types.
/// </summary>
public static class CombineExtensions
{
    // ==================== Merge Operations ====================

    /// <summary>
    /// Merges multiple Result&lt;T&gt; into a single MultiResult&lt;T&gt;.
    /// If all succeed, returns success with the first value.
    /// If any fail, returns failure with all errors.
    /// </summary>
    public static MultiResult<T> Merge<T>(params Result<T>[] results)
    {
        if (results == null || results.Length == 0)
            throw new ArgumentException("At least one result is required", nameof(results));

        // First pass: count failures and find first success value
        int failureCount = 0;
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
                failureCount++;
            }
        }

        if (failureCount == 0)
            return MultiResult<T>.Success(firstValue!);

        // Second pass: collect errors (only allocate when we have failures)
        var errors = new Error[failureCount];
        int index = 0;
        foreach (var result in results)
        {
            if (result.IsFailure)
                errors[index++] = result.Error;
        }

        return MultiResult<T>.Failure(ErrorCollection.Create(errors));
    }

    /// <summary>
    /// Merges multiple non-generic Results into a single MultiResult.
    /// </summary>
    public static MultiResult Merge(params Result[] results)
    {
        if (results == null || results.Length == 0)
            throw new ArgumentException("At least one result is required", nameof(results));

        // Count failures first to avoid allocation if all succeed
        int failureCount = 0;
        foreach (var result in results)
        {
            if (result.IsFailure)
                failureCount++;
        }

        if (failureCount == 0)
            return MultiResult.Success();

        // Only allocate when we have failures
        var errors = new Error[failureCount];
        int index = 0;
        foreach (var result in results)
        {
            if (result.IsFailure)
                errors[index++] = result.Error;
        }

        return MultiResult.Failure(ErrorCollection.Create(errors));
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
        // Check for failures without allocation
        bool hasFailure1 = result1.IsFailure;
        bool hasFailure2 = result2.IsFailure;

        if (!hasFailure1 && !hasFailure2)
            return MultiResult<(T1, T2)>.Success((result1.Value, result2.Value));

        // Only allocate array when we have failures
        int errorCount = (hasFailure1 ? 1 : 0) + (hasFailure2 ? 1 : 0);
        var errors = new Error[errorCount];
        int index = 0;

        if (hasFailure1) errors[index++] = result1.Error;
        if (hasFailure2) errors[index++] = result2.Error;

        return MultiResult<(T1, T2)>.Failure(ErrorCollection.Create(errors));
    }

    /// <summary>
    /// Combines three results, collecting all errors if any fail.
    /// </summary>
    public static MultiResult<(T1, T2, T3)> CombineAll<T1, T2, T3>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3)
    {
        // Check for failures without allocation
        bool hasFailure1 = result1.IsFailure;
        bool hasFailure2 = result2.IsFailure;
        bool hasFailure3 = result3.IsFailure;

        if (!hasFailure1 && !hasFailure2 && !hasFailure3)
            return MultiResult<(T1, T2, T3)>.Success((result1.Value, result2.Value, result3.Value));

        // Only allocate array when we have failures
        int errorCount = (hasFailure1 ? 1 : 0) + (hasFailure2 ? 1 : 0) + (hasFailure3 ? 1 : 0);
        var errors = new Error[errorCount];
        int index = 0;

        if (hasFailure1) errors[index++] = result1.Error;
        if (hasFailure2) errors[index++] = result2.Error;
        if (hasFailure3) errors[index++] = result3.Error;

        return MultiResult<(T1, T2, T3)>.Failure(ErrorCollection.Create(errors));
    }
}
