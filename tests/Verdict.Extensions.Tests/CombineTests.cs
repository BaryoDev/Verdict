using System;
using FluentAssertions;
using Xunit;
using Verdict.Extensions;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Tests for combine extensions.
/// </summary>
public class CombineTests
{
    [Fact]
    public void Combine_TwoSuccess_ShouldReturnTuple()
    {
        // Arrange
        var result1 = Result<int>.Success(42);
        var result2 = Result<string>.Success("test");

        // Act
        var combined = CombineExtensions.Combine(result1, result2);

        // Assert
        combined.IsSuccess.Should().BeTrue();
        combined.Value.Item1.Should().Be(42);
        combined.Value.Item2.Should().Be("test");
    }

    [Fact]
    public void Combine_OneFailure_ShouldReturnFirstError()
    {
        // Arrange
        var result1 = Result<int>.Failure("ERROR1", "First error");
        var result2 = Result<string>.Success("test");

        // Act
        var combined = CombineExtensions.Combine(result1, result2);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Error.Code.Should().Be("ERROR1");
    }

    [Fact]
    public void Combine_BothFailure_ShouldReturnFirstError()
    {
        // Arrange
        var result1 = Result<int>.Failure("ERROR1", "First error");
        var result2 = Result<string>.Failure("ERROR2", "Second error");

        // Act
        var combined = CombineExtensions.Combine(result1, result2);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Error.Code.Should().Be("ERROR1");
    }

    [Fact]
    public void Combine_ThreeResults_AllSuccess_ShouldReturnTuple()
    {
        // Arrange
        var result1 = Result<int>.Success(1);
        var result2 = Result<int>.Success(2);
        var result3 = Result<int>.Success(3);

        // Act
        var combined = CombineExtensions.Combine(result1, result2, result3);

        // Assert
        combined.IsSuccess.Should().BeTrue();
        combined.Value.Item1.Should().Be(1);
        combined.Value.Item2.Should().Be(2);
        combined.Value.Item3.Should().Be(3);
    }

    [Fact]
    public void CombineAll_AllSuccess_ShouldReturnSuccess()
    {
        // Arrange
        var result1 = Result<int>.Success(1);
        var result2 = Result<int>.Success(2);

        // Act
        var combined = CombineExtensions.CombineAll(result1, result2);

        // Assert
        combined.IsSuccess.Should().BeTrue();
        combined.Value.Item1.Should().Be(1);
        combined.Value.Item2.Should().Be(2);
    }

    [Fact]
    public void CombineAll_SomeFailure_ShouldCollectAllErrors()
    {
        // Arrange
        var result1 = Result<int>.Failure("E1", "Error 1");
        var result2 = Result<int>.Success(2);

        // Act
        var combined = CombineExtensions.CombineAll(result1, result2);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void CombineAll_ThreeResults_MultipleFailures_ShouldCollectAll()
    {
        // Arrange
        var result1 = Result<int>.Failure("E1", "Error 1");
        var result2 = Result<int>.Success(2);
        var result3 = Result<int>.Failure("E3", "Error 3");

        // Act
        var combined = CombineExtensions.CombineAll(result1, result2, result3);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void Merge_AllSuccess_ShouldReturnFirstValue()
    {
        // Arrange
        var results = new[]
        {
            Result<int>.Success(10),
            Result<int>.Success(20),
            Result<int>.Success(30)
        };

        // Act
        var merged = CombineExtensions.Merge(results);

        // Assert
        merged.IsSuccess.Should().BeTrue();
        merged.Value.Should().Be(10);
    }

    [Fact]
    public void Merge_SomeFailure_ShouldCollectAllErrors()
    {
        // Arrange
        var results = new[]
        {
            Result<int>.Success(10),
            Result<int>.Failure("E1", "Error 1"),
            Result<int>.Failure("E2", "Error 2")
        };

        // Act
        var merged = CombineExtensions.Merge(results);

        // Assert
        merged.IsFailure.Should().BeTrue();
        merged.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void Merge_NonGeneric_AllSuccess_ShouldReturnSuccess()
    {
        // Arrange
        var results = new[]
        {
            Result.Success(),
            Result.Success(),
            Result.Success()
        };

        // Act
        var merged = CombineExtensions.Merge(results);

        // Assert
        merged.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Merge_NonGeneric_SomeFailure_ShouldCollectErrors()
    {
        // Arrange
        var results = new[]
        {
            Result.Success(),
            Result.Failure("E1", "Error 1"),
            Result.Failure("E2", "Error 2")
        };

        // Act
        var merged = CombineExtensions.Merge(results);

        // Assert
        merged.IsFailure.Should().BeTrue();
        merged.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void Merge_WithNullArray_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => CombineExtensions.Merge((Result<int>[])null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Merge_WithEmptyArray_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => CombineExtensions.Merge(Array.Empty<Result<int>>());

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
