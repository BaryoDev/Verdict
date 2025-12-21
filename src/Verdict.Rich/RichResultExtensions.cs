using System;
using System.Collections.Generic;

namespace Verdict.Rich;

/// <summary>
/// Extension methods for adding rich metadata to Result types.
/// Metadata is stored externally and does not affect the Result struct itself.
/// </summary>
public static class RichResultExtensions
{
    // ==================== Success Metadata (Generic) ====================

    /// <summary>
    /// Adds a success message to the result.
    /// </summary>
    public static Result<T> WithSuccess<T>(this Result<T> result, string message)
    {
        if (!result.IsSuccess) return result;
        if (message == null) throw new ArgumentNullException(nameof(message));

        var metadata = ResultMetadata.GetOrCreate(result);
        metadata.Successes.Add(new SuccessInfo(message));
        return result;
    }

    /// <summary>
    /// Adds a success info with metadata to the result.
    /// </summary>
    public static Result<T> WithSuccess<T>(this Result<T> result, SuccessInfo success)
    {
        if (!result.IsSuccess) return result;

        var metadata = ResultMetadata.GetOrCreate(result);
        metadata.Successes.Add(success);
        return result;
    }

    /// <summary>
    /// Gets all success messages attached to this result.
    /// </summary>
    public static IReadOnlyList<SuccessInfo> GetSuccesses<T>(this Result<T> result)
    {
        if (!result.IsSuccess) return Array.Empty<SuccessInfo>();

        var metadata = ResultMetadata.GetOrCreate(result);
        return metadata.Successes;
    }

    // ==================== Error Metadata (Generic) ====================

    /// <summary>
    /// Adds metadata to the error.
    /// </summary>
    public static Result<T> WithErrorMetadata<T>(
        this Result<T> result,
        string key,
        object value)
    {
        if (result.IsSuccess) return result;
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        var metadata = ResultMetadata.GetOrCreate(result);
        metadata.ErrorMetadata[key] = value;
        return result;
    }

    /// <summary>
    /// Gets all error metadata attached to this result.
    /// </summary>
    public static IReadOnlyDictionary<string, object> GetErrorMetadata<T>(this Result<T> result)
    {
        if (result.IsSuccess) return new Dictionary<string, object>();

        var metadata = ResultMetadata.GetOrCreate(result);
        return metadata.ErrorMetadata;
    }

    // ==================== Success Metadata (Non-Generic) ====================

    /// <summary>
    /// Adds a success message to the result.
    /// </summary>
    public static Result WithSuccess(this Result result, string message)
    {
        if (!result.IsSuccess) return result;
        if (message == null) throw new ArgumentNullException(nameof(message));

        var metadata = ResultMetadata.GetOrCreate(result);
        metadata.Successes.Add(new SuccessInfo(message));
        return result;
    }

    /// <summary>
    /// Adds a success info with metadata to the result.
    /// </summary>
    public static Result WithSuccess(this Result result, SuccessInfo success)
    {
        if (!result.IsSuccess) return result;

        var metadata = ResultMetadata.GetOrCreate(result);
        metadata.Successes.Add(success);
        return result;
    }

    /// <summary>
    /// Gets all success messages attached to this result.
    /// </summary>
    public static IReadOnlyList<SuccessInfo> GetSuccesses(this Result result)
    {
        if (!result.IsSuccess) return Array.Empty<SuccessInfo>();

        var metadata = ResultMetadata.GetOrCreate(result);
        return metadata.Successes;
    }

    // ==================== Error Metadata (Non-Generic) ====================

    /// <summary>
    /// Adds metadata to the error.
    /// </summary>
    public static Result WithErrorMetadata(
        this Result result,
        string key,
        object value)
    {
        if (result.IsSuccess) return result;
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        var metadata = ResultMetadata.GetOrCreate(result);
        metadata.ErrorMetadata[key] = value;
        return result;
    }

    /// <summary>
    /// Gets all error metadata attached to this result.
    /// </summary>
    public static IReadOnlyDictionary<string, object> GetErrorMetadata(this Result result)
    {
        if (result.IsSuccess) return new Dictionary<string, object>();

        var metadata = ResultMetadata.GetOrCreate(result);
        return metadata.ErrorMetadata;
    }
}
