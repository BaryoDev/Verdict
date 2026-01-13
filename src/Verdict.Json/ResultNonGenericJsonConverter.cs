using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verdict.Json;

/// <summary>
/// JSON converter for non-generic Result type using System.Text.Json.
/// </summary>
public class ResultNonGenericJsonConverter : JsonConverter<Result>
{
    private readonly ErrorJsonConverter _errorConverter;

    /// <summary>
    /// Initializes a new instance of the ResultNonGenericJsonConverter class.
    /// </summary>
    public ResultNonGenericJsonConverter()
    {
        _errorConverter = new ErrorJsonConverter();
    }

    /// <summary>
    /// Initializes a new instance of the ResultNonGenericJsonConverter class with custom error converter.
    /// </summary>
    /// <param name="errorConverter">The error converter to use.</param>
    public ResultNonGenericJsonConverter(ErrorJsonConverter errorConverter)
    {
        _errorConverter = errorConverter ?? throw new ArgumentNullException(nameof(errorConverter));
    }

    /// <inheritdoc />
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        bool? isSuccess = null;
        Error error = default;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!isSuccess.HasValue)
                {
                    throw new JsonException("Missing 'isSuccess' property");
                }

                return isSuccess.Value
                    ? Result.Success()
                    : Result.Failure(error);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "issuccess":
                    isSuccess = reader.GetBoolean();
                    break;
                case "error":
                    error = _errorConverter.Read(ref reader, typeof(Error), options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Expected EndObject token");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isSuccess", value.IsSuccess);

        if (value.IsFailure)
        {
            writer.WritePropertyName("error");
            _errorConverter.Write(writer, value.Error, options);
        }

        writer.WriteEndObject();
    }
}
