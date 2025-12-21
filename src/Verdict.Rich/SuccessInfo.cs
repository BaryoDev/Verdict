using System;
using System.Collections.Generic;
using System.Linq;

namespace Verdict.Rich;

/// <summary>
/// Represents a success message with optional metadata.
/// Implemented as a readonly struct for performance.
/// </summary>
public readonly struct SuccessInfo
{
    /// <summary>
    /// Gets the success message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the metadata attached to this success message.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessInfo"/> struct.
    /// </summary>
    /// <param name="message">The success message.</param>
    public SuccessInfo(string message)
    {
        Message = message ?? string.Empty;
        Metadata = null;
    }

    private SuccessInfo(string message, IReadOnlyDictionary<string, object>? metadata)
    {
        Message = message;
        Metadata = metadata;
    }

    /// <summary>
    /// Returns a new SuccessInfo with the specified metadata added.
    /// </summary>
    public SuccessInfo WithMetadata(string key, object value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        Dictionary<string, object> metadata;
        if (Metadata != null)
        {
            metadata = new Dictionary<string, object>();
            foreach (var kvp in Metadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            metadata = new Dictionary<string, object>();
        }

        metadata[key] = value;

        return new SuccessInfo(Message, metadata);
    }

    /// <summary>
    /// Returns a string representation of this success info.
    /// </summary>
    public override string ToString()
    {
        if (Metadata == null || Metadata.Count == 0)
            return Message;

        var metadataStr = string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{Message} ({metadataStr})";
    }
}
