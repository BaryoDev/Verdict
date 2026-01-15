using System;
using System.Text.Json;
using FluentAssertions;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

public class ErrorJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ErrorJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new ErrorJsonConverter());
    }

    [Fact]
    public void Serialize_Error_ShouldProduceValidJson()
    {
        // Arrange
        var error = new Error("NOT_FOUND", "Resource not found");

        // Act
        var json = JsonSerializer.Serialize(error, _options);

        // Assert
        json.Should().Contain("\"code\":\"NOT_FOUND\"");
        json.Should().Contain("\"message\":\"Resource not found\"");
    }

    [Fact]
    public void Deserialize_ValidJson_ShouldProduceError()
    {
        // Arrange
        var json = """{"code":"NOT_FOUND","message":"Resource not found"}""";

        // Act
        var error = JsonSerializer.Deserialize<Error>(json, _options);

        // Assert
        error.Code.Should().Be("NOT_FOUND");
        error.Message.Should().Be("Resource not found");
    }

    [Fact]
    public void Serialize_ErrorWithException_ShouldNotIncludeExceptionByDefault()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = new Error("ERROR", "An error occurred", exception);

        // Act
        var json = JsonSerializer.Serialize(error, _options);

        // Assert
        json.Should().NotContain("exceptionType");
        json.Should().NotContain("exceptionMessage");
    }

    [Fact]
    public void Serialize_ErrorWithException_ShouldIncludeExceptionWhenEnabled()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ErrorJsonConverter { IncludeExceptionDetails = true });
        var exception = new InvalidOperationException("Test exception");
        var error = new Error("ERROR", "An error occurred", exception);

        // Act
        var json = JsonSerializer.Serialize(error, options);

        // Assert
        json.Should().Contain("\"exceptionType\":\"InvalidOperationException\"");
        json.Should().Contain("\"exceptionMessage\":\"Test exception\"");
    }

    [Fact]
    public void RoundTrip_Error_ShouldPreserveData()
    {
        // Arrange
        var original = new Error("VALIDATION_ERROR", "Email is invalid");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, _options);

        // Assert
        deserialized.Code.Should().Be(original.Code);
        deserialized.Message.Should().Be(original.Message);
    }
}
