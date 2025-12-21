using System;
using FluentAssertions;
using Xunit;
using Verdict.Extensions;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Tests for validation extensions.
/// </summary>
public class ValidationTests
{
    [Fact]
    public void Ensure_WhenPredicateTrue_ShouldReturnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var validated = result.Ensure(x => x > 0, "POSITIVE", "Value must be positive");

        // Assert
        validated.IsSuccess.Should().BeTrue();
        validated.Value.Should().Be(42);
    }

    [Fact]
    public void Ensure_WhenPredicateFalse_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<int>.Success(-5);

        // Act
        var validated = result.Ensure(x => x > 0, "POSITIVE", "Value must be positive");

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.Error.Code.Should().Be("POSITIVE");
        validated.Error.Message.Should().Be("Value must be positive");
    }

    [Fact]
    public void Ensure_OnFailure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("ORIGINAL", "Original error");
        var result = Result<int>.Failure(error);

        // Act
        var validated = result.Ensure(x => x > 0, "POSITIVE", "Value must be positive");

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.Error.Should().Be(error);
    }

    [Fact]
    public void Ensure_WithError_ShouldUseProvidedError()
    {
        // Arrange
        var result = Result<int>.Success(-5);
        var customError = new Error("CUSTOM", "Custom error");

        // Act
        var validated = result.Ensure(x => x > 0, customError);

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.Error.Should().Be(customError);
    }

    [Fact]
    public void EnsureAll_AllValid_ShouldReturnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var validated = result.EnsureAll(
            (x => x > 0, new Error("POSITIVE", "Must be positive")),
            (x => x < 100, new Error("MAX", "Must be less than 100")));

        // Assert
        validated.IsSuccess.Should().BeTrue();
        validated.Value.Should().Be(42);
    }

    [Fact]
    public void EnsureAll_SomeInvalid_ShouldReturnAllErrors()
    {
        // Arrange
        var result = Result<int>.Success(-5);

        // Act
        var validated = result.EnsureAll(
            (x => x > 0, new Error("POSITIVE", "Must be positive")),
            (x => x < 100, new Error("MAX", "Must be less than 100")));

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void EnsureAll_MultipleInvalid_ShouldCollectAllErrors()
    {
        // Arrange
        var result = Result<int>.Success(150);

        // Act
        var validated = result.EnsureAll(
            (x => x > 0, new Error("POSITIVE", "Must be positive")),
            (x => x < 100, new Error("MAX", "Must be less than 100")),
            (x => x % 2 == 0, new Error("EVEN", "Must be even")));

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.ErrorCount.Should().Be(1); // Only MAX fails
    }

    [Fact]
    public void ValidateAll_AllValid_ShouldReturnSuccess()
    {
        // Arrange & Act
        var result = ValidationExtensions.ValidateAll(
            42,
            (x => x > 0, "POSITIVE", "Must be positive"),
            (x => x < 100, "MAX", "Must be less than 100"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ValidateAll_SomeInvalid_ShouldCollectAllValidationErrors()
    {
        // Arrange & Act
        var result = ValidationExtensions.ValidateAll(
            -5,
            (x => x > 0, "POSITIVE", "Must be positive"),
            (x => x < 100, "MAX", "Must be less than 100"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void Ensure_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Ensure(null!, "CODE", "Message");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ensure_ChainedValidations_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<string>.Success("test@example.com");

        // Act
        var validated = result
            .Ensure(x => !string.IsNullOrEmpty(x), "REQUIRED", "Email is required")
            .Ensure(x => x.Contains("@"), "FORMAT", "Invalid email format");

        // Assert
        validated.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MultiResult_Ensure_Success_ShouldReturnSuccess()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act
        var validated = result.Ensure(x => x > 0, "POSITIVE", "Must be positive");

        // Assert
        validated.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MultiResult_Ensure_Failure_ShouldReturnFailure()
    {
        // Arrange
        var result = MultiResult<int>.Success(-5);

        // Act
        var validated = result.Ensure(x => x > 0, "POSITIVE", "Must be positive");

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.ErrorCount.Should().Be(1);
    }
}
