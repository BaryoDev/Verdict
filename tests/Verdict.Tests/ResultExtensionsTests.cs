using System;
using FluentAssertions;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Tests for ResultExtensions.
/// </summary>
public class ResultExtensionsTests
{
    [Fact]
    public void Bind_Success_ShouldChainResults()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    [Fact]
    public void Bind_Failure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_SuccessToFailure_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var error = new Error("BIND_ERROR", "Bind failed");

        // Act
        var bound = result.Bind(x => Result<string>.Failure(error));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_WithNullBinder_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Bind<int, string>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Tap_Success_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        var tapped = result.Tap(x => executed = true);

        // Assert
        executed.Should().BeTrue();
        tapped.Should().Be(result);
    }

    [Fact]
    public void Tap_Failure_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test error");
        var executed = false;

        // Act
        var tapped = result.Tap(x => executed = true);

        // Assert
        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public void Tap_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Tap(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TapError_Success_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        var tapped = result.TapError(e => executed = true);

        // Assert
        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_Failure_ShouldExecuteAction()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);
        Error? capturedError = null;

        // Act
        var tapped = result.TapError(e => capturedError = e);

        // Assert
        capturedError.Should().Be(error);
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Failure("TEST", "Test");

        // Act
        Action act = () => result.TapError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToNonGeneric_Success_ShouldConvertToNonGeneric()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var nonGeneric = result.ToNonGeneric();

        // Assert
        nonGeneric.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToNonGeneric_Failure_ShouldConvertToNonGeneric()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var nonGeneric = result.ToNonGeneric();

        // Assert
        nonGeneric.IsFailure.Should().BeTrue();
        nonGeneric.Error.Should().Be(error);
    }

    [Fact]
    public void NonGeneric_Bind_Success_ShouldChain()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var bound = result.Bind(() => Result.Success());

        // Assert
        bound.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void NonGeneric_Bind_Failure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = Result.Failure(error);

        // Act
        var bound = result.Bind(() => Result.Success());

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void NonGeneric_BindToGeneric_Success_ShouldChain()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var bound = result.Bind(() => Result<int>.Success(42));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be(42);
    }

    [Fact]
    public void ToGeneric_Success_ShouldConvertToGeneric()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var generic = result.ToGeneric();

        // Assert
        generic.IsSuccess.Should().BeTrue();
        generic.Value.Should().Be(Unit.Value);
    }

    [Fact]
    public void ToGeneric_Failure_ShouldConvertToGeneric()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = Result.Failure(error);

        // Act
        var generic = result.ToGeneric();

        // Assert
        generic.IsFailure.Should().BeTrue();
        generic.Error.Should().Be(error);
    }
    [Fact]
    public void NonGeneric_Tap_Success_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        result.Tap(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void NonGeneric_TapError_Failure_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Failure("TEST", "Test");
        var executed = false;

        // Act
        result.TapError(e => executed = true);

        // Assert
        executed.Should().BeTrue();
    }
}
