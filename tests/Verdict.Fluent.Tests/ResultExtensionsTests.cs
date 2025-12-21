using System;
using FluentAssertions;
using Xunit;
using Verdict.Fluent;

namespace Verdict.Fluent.Tests;

public class ResultExtensionsTests
{
    [Fact]
    public void Match_Success_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(
            val => val.ToString(),
            err => err.Code);

        // Assert
        output.Should().Be("42");
    }

    [Fact]
    public void Match_Failure_ShouldExecuteOnFailure()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");

        // Act
        var output = result.Match(
            val => val.ToString(),
            err => err.Code);

        // Assert
        output.Should().Be("ERR");
    }

    [Fact]
    public void OnSuccess_Success_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        result.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void OnSuccess_Failure_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");
        var executed = false;

        // Act
        result.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_Failure_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void OnFailure_Success_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void Map_Success_ShouldTransformValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public void Map_Failure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("ERR", "Msg");
        var result = Result<int>.Failure(error);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }
}
