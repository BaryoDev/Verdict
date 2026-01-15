using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

/// <summary>
/// Comprehensive edge case and security tests for JSON serialization.
/// Tests for malformed JSON, large payloads, special characters, and boundary conditions.
/// </summary>
public class SecurityEdgeCaseTests
{
    private readonly JsonSerializerOptions _options;

    public SecurityEdgeCaseTests()
    {
        _options = VerdictJsonExtensions.CreateVerdictJsonOptions();
    }

    #region Malformed JSON Deserialization

    [Fact]
    public void Deserialize_MalformedJson_ShouldThrowJsonException()
    {
        // Arrange
        var malformedJson = "{\"isSuccess\": true, \"value\": }"; // Missing value

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(malformedJson, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_IncompleteJson_ShouldThrowJsonException()
    {
        // Arrange
        var incompleteJson = "{\"isSuccess\": true"; // Incomplete JSON

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(incompleteJson, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_InvalidPropertyTypes_ShouldThrowJsonException()
    {
        // Arrange
        var invalidJson = "{\"isSuccess\": \"not_a_boolean\", \"value\": 42}";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(invalidJson, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    #endregion

    #region Very Large JSON Payloads

    [Fact]
    public void Serialize_VeryLargeSuccessValue_ShouldHandleCorrectly()
    {
        // Arrange - Create a 1MB+ string
        var largeValue = new string('X', 1024 * 1024);
        var result = Result<string>.Success(largeValue);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().HaveLength(1024 * 1024);
        deserialized.Value.Should().Be(largeValue);
    }

    [Fact]
    public void Serialize_VeryLargeErrorMessage_ShouldHandleCorrectly()
    {
        // Arrange - Create a 500KB error message
        var largeMessage = new string('E', 500 * 1024);
        var result = Result<int>.Failure("LARGE_ERROR", largeMessage);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Message.Should().HaveLength(500 * 1024);
        deserialized.Error.Code.Should().Be("LARGE_ERROR");
    }

    #endregion

    #region Unicode and Special Character Handling

    [Fact]
    public void Serialize_UnicodeCharactersInErrorCode_ShouldHandleCorrectly()
    {
        // Arrange
        var result = Result<int>.Failure("错误代码", "Unicode error code");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Code.Should().Be("错误代码");
        deserialized.Error.Message.Should().Be("Unicode error code");
    }

    [Fact]
    public void Serialize_EmojiInErrorMessage_ShouldHandleCorrectly()
    {
        // Arrange
        var result = Result<int>.Failure("EMOJI_ERROR", "Error occurred 😱🔥💥");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Message.Should().Be("Error occurred 😱🔥💥");
    }

    [Fact]
    public void Serialize_SpecialJsonCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var result = Result<string>.Success("Value with \"quotes\", \\backslashes\\ and \n newlines");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be("Value with \"quotes\", \\backslashes\\ and \n newlines");
    }

    [Fact]
    public void Serialize_ControlCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var valueWithControlChars = "Line1\tTab\rCarriage\nNewline\0Null";
        var result = Result<string>.Success(valueWithControlChars);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(valueWithControlChars);
    }

    #endregion

    #region Null Handling During Round-Trip Serialization

    [Fact]
    public void Serialize_SuccessWithNullValue_ShouldRoundTripCorrectly()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string?>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().BeNull();
    }

    [Fact]
    public void Serialize_FailureWithEmptyCode_ShouldRoundTripCorrectly()
    {
        // Arrange
        var result = Result<int>.Failure("", "Message only");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Code.Should().Be("");
        deserialized.Error.Message.Should().Be("Message only");
    }

    #endregion

    #region Empty String Serialization/Deserialization

    [Fact]
    public void Serialize_EmptyStringSuccess_ShouldRoundTripCorrectly()
    {
        // Arrange
        var result = Result<string>.Success("");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be("");
    }

    #endregion

    #region Default Struct Serialization

    [Fact]
    public void Serialize_DefaultResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Result<int> defaultResult = default;

        // Act & Assert - Default results are invalid and cannot be serialized
        Action act = () => JsonSerializer.Serialize(defaultResult, _options);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    [Fact]
    public void Serialize_NonGenericDefaultResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Result defaultResult = default;

        // Act & Assert - Default results are invalid and cannot be serialized
        Action act = () => JsonSerializer.Serialize(defaultResult, _options);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    #endregion

    #region Missing Required Fields During Deserialization

    [Fact]
    public void Deserialize_MissingIsSuccessField_ShouldThrowOrDefault()
    {
        // Arrange - JSON without isSuccess field
        var jsonWithoutIsSuccess = "{\"value\": 42}";

        // Act & Assert - Behavior depends on implementation
        // This test documents the behavior when required fields are missing
        var act = () => JsonSerializer.Deserialize<Result<int>>(jsonWithoutIsSuccess, _options);
        
        // Should either throw or create a default (failure) result
        try
        {
            var result = act();
            result.IsSuccess.Should().BeFalse(); // Defaults to failure
        }
        catch (JsonException)
        {
            // Also acceptable - missing required field
            Assert.True(true);
        }
    }

    [Fact]
    public void Deserialize_MissingValueFieldOnSuccess_ShouldThrowOrUseDefault()
    {
        // Arrange - Success JSON without value field
        var jsonWithoutValue = "{\"isSuccess\": true}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<Result<int>>(jsonWithoutValue, _options);
        
        // Should either throw or use default value
        try
        {
            var result = act();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0); // Default for int
        }
        catch (JsonException)
        {
            // Also acceptable
            Assert.True(true);
        }
    }

    [Fact]
    public void Deserialize_MissingErrorFieldsOnFailure_ShouldThrowOrUseDefaults()
    {
        // Arrange - Failure JSON without error fields
        var jsonWithoutError = "{\"isSuccess\": false}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<Result<int>>(jsonWithoutError, _options);
        
        // Should either throw or create result with empty/default error
        try
        {
            var result = act();
            result.IsFailure.Should().BeTrue();
            // Error might have default/empty values
        }
        catch (JsonException)
        {
            // Also acceptable
            Assert.True(true);
        }
    }

    #endregion

    #region Complex Nested Types

    [Fact]
    public void Serialize_ComplexNestedType_ShouldRoundTripCorrectly()
    {
        // Arrange
        var complexValue = new { Id = 1, Name = "Test", Tags = new[] { "tag1", "tag2" } };
        var result = Result<object>.Success(complexValue);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        // Note: Deserializing back to object type loses type information
        // This is expected JSON serialization behavior

        // Assert - JSON uses camelCase naming policy
        json.Should().Contain("\"id\":1");
        json.Should().Contain("\"name\":\"Test\"");
        json.Should().Contain("tag1");
        json.Should().Contain("tag2");
    }

    [Fact]
    public void Serialize_ResultOfResult_ShouldHandleNesting()
    {
        // Arrange
        var innerResult = Result<int>.Success(42);
        var outerResult = Result<Result<int>>.Success(innerResult);

        // Act
        var json = JsonSerializer.Serialize(outerResult, _options);
        var deserialized = JsonSerializer.Deserialize<Result<Result<int>>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.IsSuccess.Should().BeTrue();
        deserialized.Value.Value.Should().Be(42);
    }

    #endregion

    #region Concurrent Serialization

    [Fact]
    public void Serialize_ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var results = new Result<int>[100];
        for (int i = 0; i < 100; i++)
        {
            results[i] = Result<int>.Success(i);
        }

        // Act - Serialize concurrently
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        System.Threading.Tasks.Parallel.For(0, 100, i =>
        {
            try
            {
                var json = JsonSerializer.Serialize(results[i], _options);
                var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);
                if (!deserialized.IsSuccess || deserialized.Value != i)
                {
                    throw new InvalidOperationException($"Mismatch at index {i}");
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion
}
