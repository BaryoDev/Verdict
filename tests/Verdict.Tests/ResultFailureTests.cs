using System;
using FluentAssertions;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Tests for Result{T} failure path.
/// </summary>
public class ResultFailureTests
{
    [Fact]
    public void Failure_WithError_ShouldSetIsFailureTrue()
    {
        // Arrange
        var error = new Error("TEST", "Test error");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failure_WithError_ShouldStoreError()
    {
        // Arrange
        var expectedError = new Error("TEST", "Test error");

        // Act
        var result = Result<int>.Failure(expectedError);

        // Assert
        result.Error.Should().Be(expectedError);
    }

    [Fact]
    public void Failure_Error_ShouldReturnStoredError()
    {
        // Arrange
        var expectedError = new Error("TEST", "Test error");
        var result = Result<int>.Failure(expectedError);

        // Act
        var actualError = result.Error;

        // Assert
        actualError.Should().Be(expectedError);
        actualError.Code.Should().Be("TEST");
        actualError.Message.Should().Be("Test error");
    }

    [Fact]
    public void Failure_Value_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test error");

        // Act
        Action act = () => { var _ = result.Value; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed*");
    }

    [Fact]
    public void Failure_ValueOrDefault_ShouldReturnDefault()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test error");

        // Act
        var value = result.ValueOrDefault;

        // Assert
        value.Should().Be(default(int));
    }

    [Fact]
    public void Failure_ValueOr_ShouldReturnFallback()
    {
        // Arrange
        const int fallback = 100;
        var result = Result<int>.Failure("TEST", "Test error");

        // Act
        var value = result.ValueOr(fallback);

        // Assert
        value.Should().Be(fallback);
    }

    [Fact]
    public void Failure_ValueOr_WithFactory_ShouldReturnFactoryResult()
    {
        // Arrange
        const int fallback = 100;
        var result = Result<int>.Failure("TEST", "Test error");

        // Act
        var value = result.ValueOr(error => fallback);

        // Assert
        value.Should().Be(fallback);
    }

    [Fact]
    public void Failure_IsSuccess_ShouldBeFalse()
    {
        // Arrange & Act
        var result = Result<int>.Failure("TEST", "Test error");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_ToString_ShouldContainErrorDetails()
    {
        // Arrange
        var result = Result<int>.Failure("TEST_CODE", "Test error message");

        // Act
        var stringValue = result.ToString();

        // Assert
        stringValue.Should().Contain("Failure");
        stringValue.Should().Contain("TEST_CODE");
        stringValue.Should().Contain("Test error message");
    }

    [Fact]
    public void Failure_ImplicitConversion_FromError_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("TEST", "Test error");

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldCreateError()
    {
        // Arrange & Act
        var result = Result<int>.Failure("TEST_CODE", "Test message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TEST_CODE");
        result.Error.Message.Should().Be("Test message");
    }

    [Fact]
    public void ValueOr_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test error");

        // Act
        Action act = () => result.ValueOr(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
