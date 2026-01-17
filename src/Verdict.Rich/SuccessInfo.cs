using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Verdict.Rich;

/// <summary>
/// Represents a success message with optional metadata.
/// Implemented as a readonly struct for performance.
/// Uses ImmutableDictionary for efficient metadata additions without copying.
/// </summary>
public readonly struct SuccessInfo
{
    private readonly ImmutableDictionary<string, object>? _metadata;

    /// <summary>
    /// Gets the success message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the metadata attached to this success message.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata =>
        _metadata ?? ImmutableDictionary<string, object>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessInfo"/> struct.
    /// </summary>
    /// <param name="message">The success message.</param>
    public SuccessInfo(string message)
    {
        Message = message ?? string.Empty;
        _metadata = null;
    }

    private SuccessInfo(string message, ImmutableDictionary<string, object>? metadata)
    {
        Message = message;
        _metadata = metadata;
    }

    /// <summary>
    /// Returns a new SuccessInfo with the specified metadata added.
    /// Uses ImmutableDictionary for O(log n) additions without full copy.
    /// </summary>
    public SuccessInfo WithMetadata(string key, object value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        var newMetadata = _metadata == null
            ? ImmutableDictionary.Create<string, object>().Add(key, value)
            : _metadata.SetItem(key, value);

        return new SuccessInfo(Message, newMetadata);
    }

    /// <summary>
    /// Returns a string representation of this success info.
    /// </summary>
    public override string ToString()
    {
        if (_metadata == null || _metadata.Count == 0)
            return Message;

        // Use StringBuilder to avoid LINQ allocation
        var sb = new StringBuilder(Message);
        sb.Append(" (");
        bool first = true;
        foreach (var kvp in _metadata)
        {
            if (!first) sb.Append(", ");
            sb.Append(kvp.Key);
            sb.Append('=');
            sb.Append(kvp.Value);
            first = false;
        }
        sb.Append(')');
        return sb.ToString();
    }
}
