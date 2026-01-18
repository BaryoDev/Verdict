using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Verdict.Logging;

namespace Verdict.Logging.Tests;

/// <summary>
/// Tests for ResultLoggingExtensions.
/// </summary>
public class ResultLoggingExtensionsTests
{
    private readonly ILogger _logger;

    public ResultLoggingExtensionsTests()
    {
        _logger = NullLogger.Instance;
    }

    [Fact]
    public void Log_Success_ShouldReturnOriginalResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result.Log(_logger, "Operation completed");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
        logged.Value.Should().Be(42);
    }

    [Fact]
    public void Log_Failure_ShouldReturnOriginalResult()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var logged = result.Log(_logger, "Operation failed");

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
        logged.Error.Should().Be(error);
    }

    [Fact]
    public void Log_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Log(null!, "Message");

        // Assert - now throws to fail fast on misconfiguration
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LogSuccess_OnSuccess_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result.LogSuccess(_logger, "Success message");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LogSuccess_OnFailure_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test");

        // Act
        var logged = result.LogSuccess(_logger, "Success message");

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LogError_OnFailure_ShouldReturnResult()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var logged = result.LogError(_logger, "Error occurred");

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LogError_OnSuccess_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result.LogError(_logger, "Error message");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LogStructured_Success_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result.LogStructured(_logger, "Structured log");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LogStructured_Failure_ShouldReturnResult()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var logged = result.LogStructured(_logger, "Structured log");

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Log_NonGeneric_Success_ShouldReturnResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var logged = result.Log(_logger, "Operation completed");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Log_NonGeneric_Failure_ShouldReturnResult()
    {
        // Arrange
        var result = Result.Failure("TEST", "Test error");

        // Act
        var logged = result.Log(_logger, "Operation failed");

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LogSuccess_NonGeneric_OnSuccess_ShouldReturnResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var logged = result.LogSuccess(_logger, "Success");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LogError_NonGeneric_OnFailure_ShouldReturnResult()
    {
        // Arrange
        var result = Result.Failure("TEST", "Test");

        // Act
        var logged = result.LogError(_logger, "Error");

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Log_WithCustomLogLevels_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result.Log(_logger, "Message", LogLevel.Debug, LogLevel.Critical);

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LogSuccess_WithCustomLogLevel_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result.LogSuccess(_logger, "Message", LogLevel.Warning);

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LogError_WithCustomLogLevel_ShouldReturnResult()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test");

        // Act
        var logged = result.LogError(_logger, "Message", LogLevel.Critical);

        // Assert
        logged.Should().Be(result);
        logged.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Log_ChainedCalls_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var logged = result
            .Log(_logger, "First log")
            .LogSuccess(_logger, "Success log")
            .Log(_logger, "Final log");

        // Assert
        logged.Should().Be(result);
        logged.IsSuccess.Should().BeTrue();
    }
}
