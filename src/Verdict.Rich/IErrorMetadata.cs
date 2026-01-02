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
    public static RichResult<T> WithCustomError<T>(
        this Result<T> result,
        IErrorMetadata errorMetadata)
    {
        if (result.IsSuccess) return result;
        if (errorMetadata == null) throw new System.ArgumentNullException(nameof(errorMetadata));

        RichResult<T> richResult = result;
        
        // Add all metadata from the custom error
        foreach (var kvp in errorMetadata.GetMetadata())
        {
            richResult = richResult.WithErrorMetadata(kvp.Key, kvp.Value);
        }

        // Add error type
        richResult = richResult.WithErrorMetadata("ErrorType", errorMetadata.GetErrorType());

        return richResult;
    }

    /// <summary>
    /// Attaches custom error metadata to the result.
    /// </summary>
    public static RichResult WithCustomError(
        this Result result,
        IErrorMetadata errorMetadata)
    {
        if (result.IsSuccess) return result;
        if (errorMetadata == null) throw new System.ArgumentNullException(nameof(errorMetadata));

        RichResult richResult = result;
        
        // Add all metadata from the custom error
        foreach (var kvp in errorMetadata.GetMetadata())
        {
            richResult = richResult.WithErrorMetadata(kvp.Key, kvp.Value);
        }

        // Add error type
        richResult = richResult.WithErrorMetadata("ErrorType", errorMetadata.GetErrorType());

        return richResult;
    }
}
