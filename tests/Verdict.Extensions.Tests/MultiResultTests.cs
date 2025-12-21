using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Verdict.Extensions;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Tests for MultiResult{T}.
/// </summary>
public class MultiResultTests
{
    [Fact]
    public void MultiResult_Success_ShouldWorkLikeSingleResult()
    {
        // Arrange & Act
        var result = MultiResult<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void MultiResult_WithSingleError_ShouldExposeError()
    {
        // Arrange
        var error = new Error("TEST", "Test error");

        // Act
        var result = MultiResult<int>.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(1);
        result.Errors.ToArray().Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void MultiResult_WithMultipleErrors_ShouldExposeAll()
    {
        // Arrange
        var errors = new[]
        {
            new Error("ERROR1", "First error"),
            new Error("ERROR2", "Second error"),
            new Error("ERROR3", "Third error")
        };

        // Act
        var result = MultiResult<int>.Failure(errors);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(3);
        result.Errors.ToArray().Should().HaveCount(3);
    }

    [Fact]
    public void MultiResult_ErrorCount_ShouldBeAccurate()
    {
        // Arrange
        var errors = new[]
        {
            new Error("E1", "Error 1"),
            new Error("E2", "Error 2"),
            new Error("E3", "Error 3"),
            new Error("E4", "Error 4"),
            new Error("E5", "Error 5")
        };

        // Act
        var result = MultiResult<int>.Failure(errors);

        // Assert
        result.ErrorCount.Should().Be(5);
    }

    [Fact]
    public void MultiResult_Dispose_ShouldCleanupResources()
    {
        // Arrange
        var errors = new[]
        {
            new Error("E1", "Error 1"),
            new Error("E2", "Error 2")
        };
        var result = MultiResult<int>.Failure(errors);

        // Act
        result.Dispose();

        // Assert - should not throw
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MultiResult_ToSingleResult_Success_ShouldConvert()
    {
        // Arrange
        var multiResult = MultiResult<int>.Success(42);

        // Act
        var singleResult = multiResult.ToSingleResult();

        // Assert
        singleResult.IsSuccess.Should().BeTrue();
        singleResult.Value.Should().Be(42);
    }

    [Fact]
    public void MultiResult_ToSingleResult_Failure_ShouldReturnFirstError()
    {
        // Arrange
        var errors = new[]
        {
            new Error("FIRST", "First error"),
            new Error("SECOND", "Second error")
        };
        var multiResult = MultiResult<int>.Failure(errors);

        // Act
        var singleResult = multiResult.ToSingleResult();

        // Assert
        singleResult.IsFailure.Should().BeTrue();
        singleResult.Error.Code.Should().Be("FIRST");
    }

    [Fact]
    public void MultiResult_ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        // Arrange
        const int value = 42;

        // Act
        MultiResult<int> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void MultiResult_ImplicitConversion_FromError_ShouldCreateFailure()
    {
        // Arrange
        var error = new Error("TEST", "Test error");

        // Act
        MultiResult<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void MultiResult_ImplicitConversion_FromResult_ShouldWork()
    {
        // Arrange
        var singleResult = Result<int>.Success(42);

        // Act
        MultiResult<int> multiResult = singleResult;

        // Assert
        multiResult.IsSuccess.Should().BeTrue();
        multiResult.Value.Should().Be(42);
    }

    [Fact]
    public void MultiResult_ValueOrDefault_Success_ShouldReturnValue()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act
        var value = result.ValueOrDefault;

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void MultiResult_ValueOrDefault_Failure_ShouldReturnDefault()
    {
        // Arrange
        var result = MultiResult<int>.Failure("TEST", "Test");

        // Act
        var value = result.ValueOrDefault;

        // Assert
        value.Should().Be(default(int));
    }

    [Fact]
    public void MultiResult_Deconstruct_Success_ShouldProvideCorrectValues()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act
        var (isSuccess, value, errors) = result;

        // Assert
        isSuccess.Should().BeTrue();
        value.Should().Be(42);
        errors.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void MultiResult_Deconstruct_Failure_ShouldProvideCorrectValues()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = MultiResult<int>.Failure(error);

        // Act
        var (isSuccess, value, errors) = result;

        // Assert
        isSuccess.Should().BeFalse();
        value.Should().Be(default(int));
        errors.ToArray().Should().ContainSingle();
    }

    [Fact]
    public void MultiResult_ToString_Success_ShouldContainValue()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Success");
        str.Should().Contain("42");
    }

    [Fact]
    public void MultiResult_ToString_Failure_ShouldContainErrorCount()
    {
        // Arrange
        var result = MultiResult<int>.Failure(new Error("E1", "Error 1"), new Error("E2", "Error 2"));

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Failure");
        str.Should().Contain("2");
    }
}
