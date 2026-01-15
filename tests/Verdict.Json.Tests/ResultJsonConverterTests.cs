using System;
using System.Text.Json;
using FluentAssertions;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

public class ResultJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ResultJsonConverterTests()
    {
        _options = VerdictJsonExtensions.CreateVerdictJsonOptions();
    }

    [Fact]
    public void Serialize_SuccessResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        json.Should().Contain("\"isSuccess\":true");
        json.Should().Contain("\"value\":42");
    }

    [Fact]
    public void Serialize_FailureResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = Result<int>.Failure("NOT_FOUND", "Resource not found");

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        json.Should().Contain("\"isSuccess\":false");
        json.Should().Contain("\"code\":\"NOT_FOUND\"");
        json.Should().Contain("\"message\":\"Resource not found\"");
    }

    [Fact]
    public void Deserialize_SuccessJson_ShouldProduceSuccessResult()
    {
        // Arrange
        var json = """{"isSuccess":true,"value":42}""";

        // Act
        var result = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_FailureJson_ShouldProduceFailureResult()
    {
        // Arrange
        var json = """{"isSuccess":false,"error":{"code":"NOT_FOUND","message":"Resource not found"}}""";

        // Act
        var result = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOT_FOUND");
        result.Error.Message.Should().Be("Resource not found");
    }

    [Fact]
    public void RoundTrip_SuccessResult_ShouldPreserveData()
    {
        // Arrange
        var original = Result<string>.Success("Hello, World!");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(original.Value);
    }

    [Fact]
    public void RoundTrip_FailureResult_ShouldPreserveData()
    {
        // Arrange
        var original = Result<string>.Failure("VALIDATION_ERROR", "Name is required");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Code.Should().Be(original.Error.Code);
        deserialized.Error.Message.Should().Be(original.Error.Message);
    }

    [Fact]
    public void Serialize_ComplexType_ShouldWork()
    {
        // Arrange
        var user = new TestUser { Id = 1, Name = "John Doe" };
        var result = Result<TestUser>.Success(user);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<TestUser>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Id.Should().Be(1);
        deserialized.Value.Name.Should().Be("John Doe");
    }

    [Fact]
    public void ToJson_ExtensionMethod_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var json = result.ToJson();

        // Assert
        json.Should().Contain("\"isSuccess\":true");
        json.Should().Contain("\"value\":42");
    }

    [Fact]
    public void FromJson_StaticMethod_ShouldWork()
    {
        // Arrange
        var json = """{"isSuccess":true,"value":42}""";

        // Act
        var result = VerdictJsonExtensions.FromJson<int>(json);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_SuccessWithoutValueProperty_ShouldThrowJsonException()
    {
        // Arrange
        var json = """{"isSuccess":true}""";

        // Act
        var act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>()
            .WithMessage("Success result must contain 'value' property");
    }

    [Fact]
    public void Deserialize_SuccessWithNullReferenceTypeValue_ShouldThrowJsonException()
    {
        // Arrange
        var json = """{"isSuccess":true,"value":null}""";

        // Act
        var act = () => JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>()
            .WithMessage("Success result cannot have null value for reference types");
    }

    [Fact]
    public void Deserialize_SuccessWithNullableValueType_ShouldWork()
    {
        // Arrange - For nullable value types, null is a valid value
        var json = """{"isSuccess":true,"value":null}""";

        // Act
        var result = JsonSerializer.Deserialize<Result<int?>>(json, _options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_SuccessWithDefaultValueType_ShouldWork()
    {
        // Arrange - Value types can have their default value (0 for int)
        var json = """{"isSuccess":true,"value":0}""";

        // Act
        var result = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
