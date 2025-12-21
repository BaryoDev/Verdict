using System;
using FluentAssertions;
using Xunit;

namespace Upshot.Tests;

/// <summary>
/// Tests for Result{T} deconstruction and edge cases.
/// </summary>
public class ResultEdgeCaseTests
{
    [Fact]
    public void Deconstruct_Success_ShouldProvideCorrectValues()
    {
        // Arrange
        const int expectedValue = 42;
        var result = Result<int>.Success(expectedValue);

        // Act
        var (isSuccess, value, error) = result;

        // Assert
        isSuccess.Should().BeTrue();
        value.Should().Be(expectedValue);
        error.Should().Be(default(Error));
    }

    [Fact]
    public void Deconstruct_Failure_ShouldProvideCorrectValues()
    {
        // Arrange
        var expectedError = new Error("TEST", "Test error");
        var result = Result<int>.Failure(expectedError);

        // Act
        var (isSuccess, value, error) = result;

        // Assert
        isSuccess.Should().BeFalse();
        value.Should().Be(default(int));
        error.Should().Be(expectedError);
    }

    [Fact]
    public void Success_WithReferenceType_ShouldStoreReference()
    {
        // Arrange
        var obj = new TestClass { Value = 42 };

        // Act
        var result = Result<TestClass>.Success(obj);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(obj);
        result.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_WithException_ShouldPreserveException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = new Error("TEST", "Test error", exception);

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.Error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Result_WithStruct_ShouldWorkCorrectly()
    {
        // Arrange
        var structValue = new TestStruct { X = 10, Y = 20 };

        // Act
        var result = Result<TestStruct>.Success(structValue);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.X.Should().Be(10);
        result.Value.Y.Should().Be(20);
    }

    private class TestClass
    {
        public int Value { get; set; }
    }

    private struct TestStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
