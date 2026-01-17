using System;
using FluentAssertions;
using Xunit;
using Verdict.Extensions;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Tests for try/catch helper extensions.
/// </summary>
public class TryTests
{
    [Fact]
    public void Try_NoException_ShouldReturnSuccess()
    {
        // Arrange & Act
        var result = TryExtensions.Try(() => 42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Try_WithException_ShouldReturnFailure()
    {
        // Arrange & Act
        var result = TryExtensions.Try<int>(() => throw new InvalidOperationException("Test exception"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InvalidOperationException");
        // Default behavior is now sanitized for security - use custom errorFactory for raw message
        result.Error.Message.Should().Be("An error occurred.");
        result.Error.Exception.Should().NotBeNull();
    }

    [Fact]
    public void Try_WithCustomHandler_ShouldUseHandler()
    {
        // Arrange
        var customError = new Error("CUSTOM", "Custom error");

        // Act
        var result = TryExtensions.Try(
            () => throw new Exception("Test"),
            ex => customError);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(customError);
    }

    [Fact]
    public void Try_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => TryExtensions.Try<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Try_NonGeneric_NoException_ShouldReturnSuccess()
    {
        // Arrange
        var executed = false;

        // Act
        var result = TryExtensions.Try(() => executed = true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void Try_NonGeneric_WithException_ShouldReturnFailure()
    {
        // Arrange & Act
        var result = TryExtensions.Try(() => throw new InvalidOperationException("Test"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InvalidOperationException");
    }

    [Fact]
    public void Try_PreservesExceptionDetails()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument", "paramName");

        // Act
        var result = TryExtensions.Try<int>(() => throw exception);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Exception is always preserved for debugging
        result.Error.Exception.Should().BeSameAs(exception);
        // Message is sanitized by default for security - use custom errorFactory for raw message
        result.Error.Message.Should().Be("An error occurred.");
    }

    [Fact]
    public void Try_WithCustomErrorFactory_ShouldExposeRawMessage()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument", "paramName");

        // Act - use custom error factory to get unsanitized message
        var result = TryExtensions.Try<int>(
            () => throw exception,
            ex => Error.FromException(ex, sanitize: false));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Exception.Should().BeSameAs(exception);
        result.Error.Message.Should().Contain("Invalid argument");
    }

    [Fact]
    public void TryResult_Success_ShouldReturnResult()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult(() => Result<int>.Success(42));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void TryResult_Failure_ShouldReturnFailure()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult(() => Result<int>.Failure("TEST", "Test error"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TEST");
    }

    [Fact]
    public void TryResult_WithException_ShouldCatchAndReturnFailure()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult<int>(() => throw new Exception("Test"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Exception");
    }

    [Fact]
    public void TryResult_NonGeneric_Success_ShouldReturnSuccess()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult(() => Result.Success());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void TryResult_NonGeneric_WithException_ShouldCatchAndReturnFailure()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult(() => throw new Exception("Test"));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Try_WithCustomErrorFactory_ShouldUseFactory()
    {
        // Arrange
        var customError = new Error("CUSTOM_CODE", "Custom message");

        // Act
        var result = TryExtensions.Try(
            () => throw new Exception("Original"),
            ex => customError);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CUSTOM_CODE");
        result.Error.Message.Should().Be("Custom message");
    }
}
