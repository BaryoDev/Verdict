using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Verdict;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

/// <summary>
/// Production readiness tests for JSON serialization.
/// Tests edge cases, security scenarios, and robustness.
/// </summary>
public class ProductionReadinessTests
{
    private readonly JsonSerializerOptions _options = VerdictJsonExtensions.CreateVerdictJsonOptions();

    #region Serialization Edge Cases

    [Fact]
    public void Serialize_SuccessWithNullableValue_ShouldHandle()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<string?>>(json, _options);

        // Assert
        json.Should().Contain("\"isSuccess\":true");
        restored.IsSuccess.Should().BeTrue();
        restored.Value.Should().BeNull();
    }

    [Fact]
    public void Serialize_SuccessWithEmptyString_ShouldHandle()
    {
        // Arrange
        var result = Result<string>.Success(string.Empty);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        restored.IsSuccess.Should().BeTrue();
        restored.Value.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ErrorWithMinimalContent_ShouldHandle()
    {
        // Arrange - use minimal but valid error code
        var result = Result<int>.Failure("E", "M");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        restored.IsFailure.Should().BeTrue();
        restored.Error.Code.Should().Be("E");
        restored.Error.Message.Should().Be("M");
    }

    [Fact]
    public void Serialize_WithSpecialCharacters_ShouldEscapeProperly()
    {
        // Arrange
        var result = Result<string>.Failure("ERROR", "Message with \"quotes\" and \\ backslash");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        restored.IsFailure.Should().BeTrue();
        restored.Error.Message.Should().Contain("\"quotes\"");
        restored.Error.Message.Should().Contain("\\");
    }

    [Fact]
    public void Serialize_WithUnicodeCharacters_ShouldHandle()
    {
        // Arrange
        var result = Result<string>.Success("日本語テスト 🎉");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        restored.IsSuccess.Should().BeTrue();
        restored.Value.Should().Be("日本語テスト 🎉");
    }

    [Fact]
    public void Serialize_WithNewlines_ShouldHandle()
    {
        // Arrange
        var result = Result<string>.Failure("ERROR", "Line1\nLine2\r\nLine3");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        restored.IsFailure.Should().BeTrue();
        restored.Error.Message.Should().Be("Line1\nLine2\r\nLine3");
    }

    #endregion

    #region Large Input Tests

    [Fact]
    public void Serialize_WithVeryLargeString_ShouldHandle()
    {
        // Arrange
        var largeString = new string('x', 100_000);
        var result = Result<string>.Success(largeString);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        restored.IsSuccess.Should().BeTrue();
        restored.Value.Should().HaveLength(100_000);
    }

    [Fact]
    public void Serialize_WithVeryLongErrorMessage_ShouldHandle()
    {
        // Arrange
        var longMessage = new string('m', 50_000);
        var result = Result<int>.Failure("ERROR", longMessage);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        restored.IsFailure.Should().BeTrue();
        restored.Error.Message.Should().HaveLength(50_000);
    }

    #endregion

    #region Deserialization Robustness

    [Fact]
    public void Deserialize_MissingIsSuccess_ShouldThrow()
    {
        // Arrange
        var json = "{\"value\":42}";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldThrow()
    {
        // Arrange
        var json = "not valid json";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_EmptyObject_ShouldThrow()
    {
        // Arrange
        var json = "{}";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_SuccessWithoutValue_ShouldUseDefault()
    {
        // Arrange - success but no value field (for value types this should work)
        var json = "{\"isSuccess\":true}";

        // Act
        var result = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0); // default(int)
    }

    [Fact]
    public void Deserialize_FailureWithNullError_ShouldThrow()
    {
        // Arrange - failure with null error is invalid
        var json = "{\"isSuccess\":false,\"error\":null}";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert - should throw because failure must have error
        act.Should().Throw<JsonException>();
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_WithComplexValue_ShouldPreserve()
    {
        // Arrange - use a concrete type for proper serialization
        var result = Result<TestData>.Success(new TestData { Name = "Test", Value = 42 });

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result<TestData>>(json, _options);

        // Assert
        restored.IsSuccess.Should().BeTrue();
        restored.Value.Name.Should().Be("Test");
        restored.Value.Value.Should().Be(42);
    }

    private class TestData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Fact]
    public void RoundTrip_NonGenericResult_Success()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        restored.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_NonGenericResult_Failure()
    {
        // Arrange
        var result = Result.Failure("ERROR", "Test error");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var restored = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        restored.IsFailure.Should().BeTrue();
        restored.Error.Code.Should().Be("ERROR");
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task Serialize_ConcurrentCalls_ShouldBeSafe()
    {
        // Arrange
        var tasks = new Task<string>[100];

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                var result = Result<int>.Success(index);
                return JsonSerializer.Serialize(result, _options);
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all should succeed without exceptions
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(json => json.Should().Contain("\"isSuccess\":true"));
    }

    [Fact]
    public async Task Deserialize_ConcurrentCalls_ShouldBeSafe()
    {
        // Arrange
        var json = "{\"isSuccess\":true,\"value\":42}";
        var tasks = new Task<Result<int>>[100];

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() =>
                JsonSerializer.Deserialize<Result<int>>(json, _options));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.IsSuccess.Should().BeTrue();
            r.Value.Should().Be(42);
        });
    }

    #endregion
}
