using System;

namespace Verdict.Rich;

/// <summary>
/// Extension methods for adding rich metadata to Result types.
/// These methods convert Result to RichResult and add metadata.
/// </summary>
public static class RichResultExtensions
{
    // ==================== Conversion Methods ====================

    /// <summary>
    /// Converts a Result to a RichResult for adding metadata.
    /// </summary>
    public static RichResult<T> AsRich<T>(this Result<T> result) => result;

    /// <summary>
    /// Converts a non-generic Result to a RichResult for adding metadata.
    /// </summary>
    public static RichResult AsRich(this Result result) => result;

    // ==================== Success Metadata (Generic) ====================

    /// <summary>
    /// Adds a success message to the result.
    /// Automatically converts Result to RichResult.
    /// </summary>
    public static RichResult<T> WithSuccess<T>(this Result<T> result, string message)
    {
        RichResult<T> richResult = result;
        return richResult.WithSuccess(message);
    }

    /// <summary>
    /// Adds a success info with metadata to the result.
    /// Automatically converts Result to RichResult.
    /// </summary>
    public static RichResult<T> WithSuccess<T>(this Result<T> result, SuccessInfo success)
    {
        RichResult<T> richResult = result;
        return richResult.WithSuccess(success);
    }

    // ==================== Error Metadata (Generic) ====================

    /// <summary>
    /// Adds metadata to the error.
    /// Automatically converts Result to RichResult.
    /// </summary>
    public static RichResult<T> WithErrorMetadata<T>(
        this Result<T> result,
        string key,
        object value)
    {
        RichResult<T> richResult = result;
        return richResult.WithErrorMetadata(key, value);
    }

    // ==================== Success Metadata (Non-Generic) ====================

    /// <summary>
    /// Adds a success message to the result.
    /// Automatically converts Result to RichResult.
    /// </summary>
    public static RichResult WithSuccess(this Result result, string message)
    {
        RichResult richResult = result;
        return richResult.WithSuccess(message);
    }

    /// <summary>
    /// Adds a success info with metadata to the result.
    /// Automatically converts Result to RichResult.
    /// </summary>
    public static RichResult WithSuccess(this Result result, SuccessInfo success)
    {
        RichResult richResult = result;
        return richResult.WithSuccess(success);
    }

    // ==================== Error Metadata (Non-Generic) ====================

    /// <summary>
    /// Adds metadata to the error.
    /// Automatically converts Result to RichResult.
    /// </summary>
    public static RichResult WithErrorMetadata(
        this Result result,
        string key,
        object value)
    {
        RichResult richResult = result;
        return richResult.WithErrorMetadata(key, value);
    }
}
