using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verdict.Json;

/// <summary>
/// JSON converter for Result&lt;T&gt; type using System.Text.Json.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public class ResultJsonConverter<T> : JsonConverter<Result<T>>
{
    private readonly ErrorJsonConverter _errorConverter;

    /// <summary>
    /// Initializes a new instance of the ResultJsonConverter class.
    /// </summary>
    public ResultJsonConverter()
    {
        _errorConverter = new ErrorJsonConverter();
    }

    /// <summary>
    /// Initializes a new instance of the ResultJsonConverter class with custom error converter.
    /// </summary>
    /// <param name="errorConverter">The error converter to use.</param>
    public ResultJsonConverter(ErrorJsonConverter errorConverter)
    {
        _errorConverter = errorConverter ?? throw new ArgumentNullException(nameof(errorConverter));
    }

    /// <inheritdoc />
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        bool? isSuccess = null;
        T? value = default;
        Error error = default;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!isSuccess.HasValue)
                {
                    throw new JsonException("Missing 'isSuccess' property");
                }

                if (isSuccess.Value)
                {
                    return Result<T>.Success(value!);
                }
                
                // Validate error is not in default state for failure results
                if (string.IsNullOrEmpty(error.Code) && string.IsNullOrEmpty(error.Message))
                {
                    throw new JsonException("Missing 'error' property for failure result (isSuccess=false)");
                }

                return Result<T>.Failure(error);
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
                case "value":
                    value = JsonSerializer.Deserialize<T>(ref reader, options);
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
    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isSuccess", value.IsSuccess);

        if (value.IsSuccess)
        {
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WritePropertyName("error");
            _errorConverter.Write(writer, value.Error, options);
        }

        writer.WriteEndObject();
    }
}
