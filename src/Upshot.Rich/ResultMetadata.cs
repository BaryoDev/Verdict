using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Upshot.Rich;

/// <summary>
/// Internal storage for result metadata.
/// Stores success messages and error metadata separately from the Result struct.
/// </summary>
internal class MetadataStore
{
    /// <summary>
    /// Success messages attached to this result.
    /// </summary>
    public List<SuccessInfo> Successes { get; } = new();

    /// <summary>
    /// Error metadata attached to this result.
    /// </summary>
    public Dictionary<string, object> ErrorMetadata { get; } = new();
}

/// <summary>
/// Manages external metadata storage for Result types.
/// Uses ConditionalWeakTable for automatic cleanup when results are garbage collected.
/// </summary>
public static class ResultMetadata
{
    private static readonly ConditionalWeakTable<object, MetadataStore> _metadata = new();

    /// <summary>
    /// Gets or creates metadata storage for the specified result.
    /// </summary>
    internal static MetadataStore GetOrCreate<T>(Result<T> result)
    {
        // Box the struct to use as a key
        // ConditionalWeakTable will automatically clean up when the boxed result is GC'd
        var key = (object)result;
        return _metadata.GetOrCreateValue(key);
    }

    /// <summary>
    /// Gets or creates metadata storage for the specified non-generic result.
    /// </summary>
    internal static MetadataStore GetOrCreate(Result result)
    {
        var key = (object)result;
        return _metadata.GetOrCreateValue(key);
    }
}
