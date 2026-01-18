using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Verdict.Logging;

namespace Verdict.Logging.Tests;

/// <summary>
/// Tests for ResultLogger.
/// </summary>
public class ResultLoggerTests
{
    private readonly ILogger _logger;

    public ResultLoggerTests()
    {
        _logger = NullLogger.Instance;
    }

    [Fact]
    public void Create_Success_ShouldReturnSuccessResult()
    {
        // Arrange & Act
        var result = ResultLogger.Create(_logger, () => Result<int>.Success(42), "TestOperation");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Create_Failure_ShouldReturnFailureResult()
    {
        // Arrange & Act
        var result = ResultLogger.Create(_logger, () => Result<int>.Failure("TEST", "Test error"), "TestOperation");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TEST");
    }

    [Fact]
    public void Create_WithException_ShouldRethrow()
    {
        // Arrange & Act
        Action act = () => ResultLogger.Create<int>(_logger, () => throw new InvalidOperationException("Test"), "TestOperation");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Test");
    }

    [Fact]
    public void Create_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => ResultLogger.Create(null!, () => Result<int>.Success(42), "TestOperation");

        // Assert - now throws to fail fast on misconfiguration
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => ResultLogger.Create<int>(_logger, null!, "TestOperation");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_Success_ShouldReturnSuccessResult()
    {
        // Arrange & Act
        var result = await ResultLogger.CreateAsync(_logger, () => Task.FromResult(Result<int>.Success(42)), "TestOperation");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_Failure_ShouldReturnFailureResult()
    {
        // Arrange & Act
        var result = await ResultLogger.CreateAsync(_logger, () => Task.FromResult(Result<int>.Failure("TEST", "Test")), "TestOperation");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TEST");
    }

    [Fact]
    public async Task CreateAsync_WithException_ShouldRethrow()
    {
        // Arrange & Act
        Func<Task> act = async () => await ResultLogger.CreateAsync<int>(_logger, () => throw new InvalidOperationException("Test"), "TestOperation");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Create_NonGeneric_Success_ShouldReturnSuccessResult()
    {
        // Arrange & Act
        var result = ResultLogger.Create(_logger, () => Result.Success(), "TestOperation");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_NonGeneric_Success_ShouldReturnSuccessResult()
    {
        // Arrange & Act
        var result = await ResultLogger.CreateAsync(_logger, () => Task.FromResult(Result.Success()), "TestOperation");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
