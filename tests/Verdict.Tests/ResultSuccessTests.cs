using System;
using FluentAssertions;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Tests for Result{T} success path.
/// </summary>
public class ResultSuccessTests
{
    [Fact]
    public void Success_WithValue_ShouldSetIsSuccessTrue()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_WithValue_ShouldStoreValue()
    {
        // Arrange
        const int expectedValue = 42;

        // Act
        var result = Result<int>.Success(expectedValue);

        // Assert
        result.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Success_Value_ShouldReturnStoredValue()
    {
        // Arrange
        const string expectedValue = "test";
        var result = Result<string>.Success(expectedValue);

        // Act
        var actualValue = result.Value;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void Success_ValueOrDefault_ShouldReturnValue()
    {
        // Arrange
        const int expectedValue = 42;
        var result = Result<int>.Success(expectedValue);

        // Act
        var actualValue = result.ValueOrDefault;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void Success_ValueOr_ShouldReturnValue()
    {
        // Arrange
        const int expectedValue = 42;
        const int fallback = 100;
        var result = Result<int>.Success(expectedValue);

        // Act
        var actualValue = result.ValueOr(fallback);

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void Success_Error_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => { var _ = result.Error; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*success*");
    }

    [Fact]
    public void Success_IsFailure_ShouldBeFalse()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Success_ToString_ShouldContainValue()
    {
        // Arrange
        const int value = 42;
        var result = Result<int>.Success(value);

        // Act
        var stringValue = result.ToString();

        // Assert
        stringValue.Should().Contain("Success");
        stringValue.Should().Contain("42");
    }

    [Fact]
    public void Success_ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        const int value = 42;

        // Act
        Result<int> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Success_WithNullValue_WhenTIsNullable_ShouldSucceed()
    {
        // Arrange & Act
        Result<string?> result = Result<string?>.Success(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
