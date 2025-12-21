using System;
using FluentAssertions;
using Xunit;

namespace Upshot.Tests;

/// <summary>
/// Tests for non-generic Result.
/// </summary>
public class ResultNonGenericTests
{
    [Fact]
    public void Result_Success_ShouldSetIsSuccessTrue()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Result_Failure_ShouldSetIsFailureTrue()
    {
        // Arrange & Act
        var result = Result.Failure("TEST", "Test error");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Result_Failure_ShouldStoreError()
    {
        // Arrange
        var error = new Error("TEST", "Test error");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Result_Success_Error_ShouldThrow()
    {
        // Arrange
        var result = Result.Success();

        // Act
        Action act = () => { var _ = result.Error; };

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
