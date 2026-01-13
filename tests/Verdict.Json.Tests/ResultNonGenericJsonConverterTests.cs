using System.Text.Json;
using FluentAssertions;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

public class ResultNonGenericJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ResultNonGenericJsonConverterTests()
    {
        _options = VerdictJsonExtensions.CreateVerdictJsonOptions();
    }

    [Fact]
    public void Serialize_SuccessResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        json.Should().Contain("\"isSuccess\":true");
    }

    [Fact]
    public void Serialize_FailureResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = Result.Failure("ERROR", "Something went wrong");

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        json.Should().Contain("\"isSuccess\":false");
        json.Should().Contain("\"code\":\"ERROR\"");
    }

    [Fact]
    public void Deserialize_SuccessJson_ShouldProduceSuccessResult()
    {
        // Arrange
        var json = """{"isSuccess":true}""";

        // Act
        var result = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_FailureJson_ShouldProduceFailureResult()
    {
        // Arrange
        var json = """{"isSuccess":false,"error":{"code":"ERROR","message":"Something went wrong"}}""";

        // Act
        var result = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ERROR");
        result.Error.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void RoundTrip_SuccessResult_ShouldPreserveData()
    {
        // Arrange
        var original = Result.Success();

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_FailureResult_ShouldPreserveData()
    {
        // Arrange
        var original = Result.Failure("VALIDATION_ERROR", "Validation failed");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Code.Should().Be("VALIDATION_ERROR");
        deserialized.Error.Message.Should().Be("Validation failed");
    }

    [Fact]
    public void ToJson_ExtensionMethod_ShouldWork()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var json = result.ToJson();

        // Assert
        json.Should().Contain("\"isSuccess\":true");
    }

    [Fact]
    public void ResultFromJson_StaticMethod_ShouldWork()
    {
        // Arrange
        var json = """{"isSuccess":true}""";

        // Act
        var result = VerdictJsonExtensions.ResultFromJson(json);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
