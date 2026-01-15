using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verdict.Json;

/// <summary>
/// JSON converter for Error type using System.Text.Json.
/// </summary>
public class ErrorJsonConverter : JsonConverter<Error>
{
    /// <summary>
    /// Gets or sets a value indicating whether to include exception details in the serialized error.
    /// Set this to <c>true</c> only in trusted or development environments where exposing internal
    /// exception information is acceptable; it should remain <c>false</c> in production for security reasons.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;

    /// <inheritdoc />
    public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        string code = string.Empty;
        string message = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new Error(code, message);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "code":
                    code = reader.GetString() ?? string.Empty;
                    break;
                case "message":
                    message = reader.GetString() ?? string.Empty;
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Expected EndObject token");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("code", value.Code);
        writer.WriteString("message", value.Message);

        if (IncludeExceptionDetails && value.Exception != null)
        {
            writer.WriteString("exceptionType", value.Exception.GetType().Name);
            writer.WriteString("exceptionMessage", value.Exception.Message);
        }

        writer.WriteEndObject();
    }
}
