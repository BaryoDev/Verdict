using System.Collections.Generic;
using System.Collections.Immutable;

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
    /// Builds the metadata dictionary in one shot to avoid O(N) intermediate allocations.
    /// </summary>
    public static RichResult<T> WithCustomError<T>(
        this Result<T> result,
        IErrorMetadata errorMetadata)
    {
        if (result.IsSuccess) return result;
        if (errorMetadata == null) throw new System.ArgumentNullException(nameof(errorMetadata));

        var builder = ImmutableDictionary.CreateBuilder<string, object>();
        foreach (var kvp in errorMetadata.GetMetadata())
        {
            builder[kvp.Key] = kvp.Value;
        }
        builder["ErrorType"] = errorMetadata.GetErrorType();

        RichResult<T> richResult = result;
        return richResult.WithErrorMetadataBulk(builder.ToImmutable());
    }

    /// <summary>
    /// Attaches custom error metadata to the result.
    /// Builds the metadata dictionary in one shot to avoid O(N) intermediate allocations.
    /// </summary>
    public static RichResult WithCustomError(
        this Result result,
        IErrorMetadata errorMetadata)
    {
        if (result.IsSuccess) return result;
        if (errorMetadata == null) throw new System.ArgumentNullException(nameof(errorMetadata));

        var builder = ImmutableDictionary.CreateBuilder<string, object>();
        foreach (var kvp in errorMetadata.GetMetadata())
        {
            builder[kvp.Key] = kvp.Value;
        }
        builder["ErrorType"] = errorMetadata.GetErrorType();

        RichResult richResult = result;
        return richResult.WithErrorMetadataBulk(builder.ToImmutable());
    }
}
