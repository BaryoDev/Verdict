using System.Text.Json;

namespace Verdict.Json;

/// <summary>
/// Extension methods for configuring JSON serialization with Verdict types.
/// </summary>
public static class VerdictJsonExtensions
{
    /// <summary>
    /// Adds Verdict JSON converters to the serializer options.
    /// </summary>
    /// <param name="options">The JSON serializer options.</param>
    /// <param name="includeExceptionDetails">Whether to include exception details in serialization.</param>
    /// <returns>The JSON serializer options for chaining.</returns>
    public static JsonSerializerOptions AddVerdictConverters(
        this JsonSerializerOptions options,
        bool includeExceptionDetails = false)
    {
        options.Converters.Add(new ErrorJsonConverter { IncludeExceptionDetails = includeExceptionDetails });
        options.Converters.Add(new ResultJsonConverterFactory(includeExceptionDetails));
        return options;
    }

    /// <summary>
    /// Creates JSON serializer options configured for Verdict types.
    /// </summary>
    /// <param name="includeExceptionDetails">Whether to include exception details in serialization.</param>
    /// <returns>Configured JSON serializer options.</returns>
    public static JsonSerializerOptions CreateVerdictJsonOptions(bool includeExceptionDetails = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        return options.AddVerdictConverters(includeExceptionDetails);
    }

    /// <summary>
    /// Serializes a Result to JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>JSON string representation of the result.</returns>
    public static string ToJson<T>(this Result<T> result, JsonSerializerOptions? options = null)
    {
        options ??= CreateVerdictJsonOptions();
        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Serializes a non-generic Result to JSON string.
    /// </summary>
    /// <param name="result">The result to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>JSON string representation of the result.</returns>
    public static string ToJson(this Result result, JsonSerializerOptions? options = null)
    {
        options ??= CreateVerdictJsonOptions();
        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Deserializes a JSON string to Result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>Deserialized Result.</returns>
    public static Result<T> FromJson<T>(string json, JsonSerializerOptions? options = null)
    {
        options ??= CreateVerdictJsonOptions();
        return JsonSerializer.Deserialize<Result<T>>(json, options);
    }

    /// <summary>
    /// Deserializes a JSON string to non-generic Result.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>Deserialized Result.</returns>
    public static Result ResultFromJson(string json, JsonSerializerOptions? options = null)
    {
        options ??= CreateVerdictJsonOptions();
        return JsonSerializer.Deserialize<Result>(json, options);
    }
}
