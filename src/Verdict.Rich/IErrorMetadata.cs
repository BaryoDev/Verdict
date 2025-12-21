using System.Collections.Generic;

namespace Verdict.Rich;

/// <summary>
/// Interface for custom error metadata.
/// Implement this interface to create strongly-typed error metadata.
/// </summary>
public interface IErrorMetadata
{
    /// <summary>
    /// Gets the error type identifier.
    /// </summary>
    string GetErrorType();

    /// <summary>
    /// Gets the metadata dictionary.
    /// </summary>
    Dictionary<string, object> GetMetadata();
}

/// <summary>
/// Extension methods for custom error types.
/// </summary>
public static class CustomErrorExtensions
{
    /// <summary>
    /// Attaches custom error metadata to the result.
    /// </summary>
    public static Result<T> WithCustomError<T>(
        this Result<T> result,
        IErrorMetadata errorMetadata)
    {
        if (result.IsSuccess) return result;
        if (errorMetadata == null) throw new System.ArgumentNullException(nameof(errorMetadata));

        var metadata = ResultMetadata.GetOrCreate(result);
        foreach (var kvp in errorMetadata.GetMetadata())
        {
            metadata.ErrorMetadata[kvp.Key] = kvp.Value;
        }

        // Add error type
        metadata.ErrorMetadata["ErrorType"] = errorMetadata.GetErrorType();

        return result;
    }

    /// <summary>
    /// Attaches custom error metadata to the result.
    /// </summary>
    public static Result WithCustomError(
        this Result result,
        IErrorMetadata errorMetadata)
    {
        if (result.IsSuccess) return result;
        if (errorMetadata == null) throw new System.ArgumentNullException(nameof(errorMetadata));

        var metadata = ResultMetadata.GetOrCreate(result);
        foreach (var kvp in errorMetadata.GetMetadata())
        {
            metadata.ErrorMetadata[kvp.Key] = kvp.Value;
        }

        // Add error type
        metadata.ErrorMetadata["ErrorType"] = errorMetadata.GetErrorType();

        return result;
    }
}
