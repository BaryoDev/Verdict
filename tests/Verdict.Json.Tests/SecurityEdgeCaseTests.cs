using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

/// <summary>
/// Comprehensive edge case and security tests for JSON serialization.
/// Tests for malformed JSON, large payloads, special characters, and concurrent serialization.
/// </summary>
public class SecurityEdgeCaseTests
{
    private readonly JsonSerializerOptions _options;

    public SecurityEdgeCaseTests()
    {
        _options = VerdictJsonExtensions.CreateVerdictJsonOptions();
    }

    #region Malformed JSON Tests

    [Fact]
    public void Deserialize_IncompleteJson_ShouldThrowJsonException()
    {
        // Arrange
        var json = """{"isSuccess":true,"value":""";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_InvalidTypeForValue_ShouldThrowJsonException()
    {
        // Arrange
        var json = """{"isSuccess":true,"value":"not_an_int"}""";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_MissingIsSuccessProperty_ShouldThrowJsonException()
    {
        // Arrange
        var json = """{"value":42}""";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>()
            .WithMessage("*isSuccess*");
    }

    [Fact]
    public void Deserialize_InvalidJsonStructure_ShouldThrowJsonException()
    {
        // Arrange
        var json = """[{"isSuccess":true}]""";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_EmptyObject_ShouldThrowJsonException()
    {
        // Arrange
        var json = """{}""";

        // Act
        Action act = () => JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    #endregion

    #region Large Payload Tests

    [Fact]
    public void Serialize_LargeStringValue_ShouldHandle()
    {
        // Arrange
        var largeValue = new string('X', 1_000_000); // 1MB string
        var result = Result<string>.Success(largeValue);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().HaveLength(1_000_000);
    }

    [Fact]
    public void Serialize_LargeErrorMessage_ShouldHandle()
    {
        // Arrange
        var largeMessage = new string('E', 500_000); // 500KB message
        var result = Result<int>.Failure("LARGE_ERROR", largeMessage);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Message.Should().HaveLength(500_000);
    }

    [Fact]
    public void Serialize_DeeplyNestedObject_ShouldHandle()
    {
        // Arrange
        var nested = new NestedObject
        {
            Level1 = new Level1
            {
                Level2 = new Level2
                {
                    Level3 = new Level3
                    {
                        Value = "Deep value"
                    }
                }
            }
        };
        var result = Result<NestedObject>.Success(nested);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<NestedObject>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Level1.Level2.Level3.Value.Should().Be("Deep value");
    }

    #endregion

    #region Special Characters Tests

    [Fact]
    public void RoundTrip_UnicodeCharacters_ShouldPreserve()
    {
        // Arrange
        var unicodeValue = "Hello \u4e16\u754c \u0416\u0438\u0437\u043d\u044c"; // Chinese and Russian
        var result = Result<string>.Success(unicodeValue);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(unicodeValue);
    }

    [Fact]
    public void RoundTrip_Emojis_ShouldPreserve()
    {
        // Arrange
        var emojiValue = "Status: \U0001F600\U0001F389\U0001F680"; // Emoji characters
        var result = Result<string>.Success(emojiValue);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(emojiValue);
    }

    [Fact]
    public void RoundTrip_ControlCharacters_ShouldPreserve()
    {
        // Arrange
        var controlChars = "Line1\nLine2\tTabbed\rCarriage";
        var result = Result<string>.Success(controlChars);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(controlChars);
    }

    [Fact]
    public void RoundTrip_EscapedJsonCharacters_ShouldPreserve()
    {
        // Arrange
        var escapedValue = "Quote: \" Backslash: \\ Forward: /";
        var result = Result<string>.Success(escapedValue);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(escapedValue);
    }

    [Fact]
    public void RoundTrip_ErrorWithSpecialCharacters_ShouldPreserve()
    {
        // Arrange
        var specialCode = "ERR_\u00C9\u00C8\u00CA";
        var specialMessage = "Error: \"quoted\" and 'single' with \\ backslash";
        var result = Result<int>.Failure(specialCode, specialMessage);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailure.Should().BeTrue();
        deserialized.Error.Code.Should().Be(specialCode);
        deserialized.Error.Message.Should().Be(specialMessage);
    }

    #endregion

    #region Null/Empty Handling Tests

    [Fact]
    public void RoundTrip_NullableValue_Null_ShouldPreserve()
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
    public void RoundTrip_EmptyString_ShouldPreserve()
    {
        // Arrange
        var result = Result<string>.Success(string.Empty);

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void RoundTrip_WhitespaceString_ShouldPreserve()
    {
        // Arrange
        var result = Result<string>.Success("   ");

        // Act
        var json = JsonSerializer.Serialize(result, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be("   ");
    }

    #endregion

    #region Default Struct Tests

    [Fact]
    public void Serialize_DefaultResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var defaultResult = default(Result<int>);

        // Act
        Action act = () => JsonSerializer.Serialize(defaultResult, _options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    #endregion

    #region Missing Fields Tests

    [Fact]
    public void Deserialize_SuccessWithExtraFields_ShouldIgnoreExtras()
    {
        // Arrange
        var json = """{"isSuccess":true,"value":42,"extraField":"ignored","anotherExtra":123}""";

        // Act
        var result = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_FailureWithExtraFields_ShouldIgnoreExtras()
    {
        // Arrange
        var json = """{"isSuccess":false,"error":{"code":"ERR","message":"Msg"},"extraField":"ignored"}""";

        // Act
        var result = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ERR");
    }

    #endregion

    #region Concurrent Serialization Tests

    [Fact]
    public async Task ConcurrentSerialization_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var syncLock = new object();

        // Act - Run 100 parallel serialization/deserialization operations
        for (int i = 0; i < 100; i++)
        {
            var localI = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var result = Result<int>.Success(localI);
                    var json = JsonSerializer.Serialize(result, _options);
                    var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

                    if (!deserialized.IsSuccess || deserialized.Value != localI)
                    {
                        throw new InvalidOperationException($"Mismatch: expected {localI}, got {deserialized.Value}");
                    }
                }
                catch (Exception ex)
                {
                    lock (syncLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentSerialization_MixedSuccessAndFailure_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var syncLock = new object();

        // Act - Run 100 parallel operations with mixed success/failure
        for (int i = 0; i < 100; i++)
        {
            var localI = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Result<int> result;
                    if (localI % 2 == 0)
                    {
                        result = Result<int>.Success(localI);
                    }
                    else
                    {
                        result = Result<int>.Failure($"ERR_{localI}", $"Error {localI}");
                    }

                    var json = JsonSerializer.Serialize(result, _options);
                    var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

                    if (localI % 2 == 0)
                    {
                        if (!deserialized.IsSuccess || deserialized.Value != localI)
                        {
                            throw new InvalidOperationException($"Success mismatch at {localI}");
                        }
                    }
                    else
                    {
                        if (!deserialized.IsFailure || deserialized.Error.Code != $"ERR_{localI}")
                        {
                            throw new InvalidOperationException($"Failure mismatch at {localI}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (syncLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion

    #region Helper Types

    private class NestedObject
    {
        public Level1 Level1 { get; set; } = new();
    }

    private class Level1
    {
        public Level2 Level2 { get; set; } = new();
    }

    private class Level2
    {
        public Level3 Level3 { get; set; } = new();
    }

    private class Level3
    {
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
