using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verdict.Json;

/// <summary>
/// Factory for creating Result JSON converters.
/// Required for generic type support in System.Text.Json.
/// </summary>
public class ResultJsonConverterFactory : JsonConverterFactory
{
    private readonly ErrorJsonConverter _errorConverter;

    /// <summary>
    /// Initializes a new instance of the ResultJsonConverterFactory class.
    /// </summary>
    public ResultJsonConverterFactory()
    {
        _errorConverter = new ErrorJsonConverter();
    }

    /// <summary>
    /// Initializes a new instance of the ResultJsonConverterFactory class.
    /// </summary>
    /// <param name="includeExceptionDetails">Whether to include exception details in serialization.</param>
    public ResultJsonConverterFactory(bool includeExceptionDetails)
    {
        _errorConverter = new ErrorJsonConverter { IncludeExceptionDetails = includeExceptionDetails };
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(Result))
        {
            return true;
        }

        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Result))
        {
            return new ResultNonGenericJsonConverter(_errorConverter);
        }

        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(ResultJsonConverter<>).MakeGenericType(valueType);
            return (JsonConverter?)Activator.CreateInstance(converterType, _errorConverter);
        }

        throw new InvalidOperationException($"Cannot create converter for {typeToConvert}");
    }
}
