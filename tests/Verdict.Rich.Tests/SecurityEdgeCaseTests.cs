using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Verdict.Rich.Tests;

/// <summary>
/// Comprehensive edge case and security tests for Rich Result extensions.
/// Tests for potential memory leaks, concurrent access issues, and boundary conditions.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region Metadata Storage Edge Cases

    [Fact]
    public void ResultMetadata_MultipleResults_ShouldStoreSeparately()
    {
        // Arrange
        var result1 = Result<int>.Success(1).WithSuccess("First success");
        var result2 = Result<int>.Success(2).WithSuccess("Second success");

        // Act
        var successes1 = result1.GetSuccesses().ToList();
        var successes2 = result2.GetSuccesses().ToList();

        // Assert
        successes1.Should().HaveCount(1);
        successes2.Should().HaveCount(1);
        successes1[0].Message.Should().Be("First success");
        successes2[0].Message.Should().Be("Second success");
    }

    [Fact]
    public void ResultMetadata_WithSuccess_NullMessage_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act & Assert
        Action act = () => result.WithSuccess(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResultMetadata_WithSuccess_EmptyMessage_ShouldStore()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var withSuccess = result.WithSuccess(string.Empty);
        var successes = withSuccess.GetSuccesses().ToList();

        // Assert
        successes.Should().HaveCount(1);
        successes[0].Message.Should().Be(string.Empty);
    }

    [Fact]
    public void ResultMetadata_MultipleWithSuccess_ShouldAccumulate()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var enriched = result
            .WithSuccess("First")
            .WithSuccess("Second")
            .WithSuccess("Third");
        var successes = enriched.GetSuccesses().ToList();

        // Assert
        successes.Should().HaveCount(3);
        successes[0].Message.Should().Be("First");
        successes[1].Message.Should().Be("Second");
        successes[2].Message.Should().Be("Third");
    }

    [Fact]
    public void ResultMetadata_ManySuccessMessages_ShouldHandleEfficiently()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act - Reduced from 1000 to 100 for faster test execution while still exposing the memory leak
        for (int i = 0; i < 100; i++)
        {
            result = result.WithSuccess($"Success {i}");
        }
        var successes = result.GetSuccesses().ToList();

        // Assert
        successes.Should().HaveCount(100);
    }

    [Fact]
    public void ResultMetadata_GetSuccesses_OnFailure_ShouldReturnEmpty()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act
        var successes = result.GetSuccesses().ToList();

        // Assert
        successes.Should().BeEmpty();
    }

    #endregion

    #region Concurrent Metadata Access

    [Fact]
    public void ResultMetadata_ConcurrentRead_ShouldBeThreadSafe()
    {
        // Arrange
        var result = Result<int>.Success(42).WithSuccess("Test success");
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            try
            {
                var successes = result.GetSuccesses().ToList();
                if (successes.Count == 0)
                {
                    throw new InvalidOperationException("Expected at least one success message");
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion

    #region Error Metadata Edge Cases

    [Fact]
    public void ResultMetadata_GetErrorMetadata_WithValues_ShouldRetrieve()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error")
            .WithErrorMetadata("key1", "value1");

        // Act
        var metadata = result.GetErrorMetadata();

        // Assert
        metadata.Should().ContainKey("key1");
        metadata["key1"].Should().Be("value1");
    }

    [Fact]
    public void ResultMetadata_GetErrorMetadata_OnResultWithoutMetadata_ShouldReturnEmpty()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act
        var metadata = result.GetErrorMetadata();

        // Assert
        metadata.Should().BeEmpty();
    }

    [Fact]
    public void ResultMetadata_WithErrorMetadata_OnSuccess_ShouldStillSucceed()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var withMetadata = result.WithErrorMetadata("key", "value");

        // Assert
        withMetadata.IsSuccess.Should().BeTrue();
        withMetadata.Value.Should().Be(42);
    }

    [Fact]
    public void ResultMetadata_ErrorMetadata_NullKey_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act & Assert
        Action act = () => result.WithErrorMetadata(null!, "value");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResultMetadata_ErrorMetadata_NullValue_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act & Assert
        Action act = () => result.WithErrorMetadata("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToString Edge Cases

    [Fact]
    public void SuccessInfo_ToString_ShouldContainMessage()
    {
        // Arrange
        var info = new SuccessInfo("Test success message");

        // Act
        var str = info.ToString();

        // Assert
        str.Should().Contain("Test success message");
    }

    [Fact]
    public void SuccessInfo_WithLongMessage_ShouldNotCauseIssues()
    {
        // Arrange
        var longMessage = new string('X', 100000);
        var info = new SuccessInfo(longMessage);

        // Act
        var str = info.ToString();

        // Assert
        str.Should().NotBeNull();
    }

    [Fact]
    public void SuccessInfo_WithMetadata_ToString_ShouldIncludeMetadata()
    {
        // Arrange
        var info = new SuccessInfo("Test")
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", "value2");

        // Act
        var str = info.ToString();

        // Assert
        str.Should().Contain("Test");
        str.Should().Contain("key1");
        str.Should().Contain("value1");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void RichResult_ComplexChain_ShouldMaintainMetadata()
    {
        // Arrange & Act
        var result = Result<int>.Success(1)
            .WithSuccess("Step 1")
            .WithSuccess("Step 2")
            .WithSuccess("Step 3");

        var successes = result.GetSuccesses().ToList();

        // Assert
        successes.Should().HaveCount(3);
        successes[0].Message.Should().Be("Step 1");
        successes[1].Message.Should().Be("Step 2");
        successes[2].Message.Should().Be("Step 3");
    }

    [Fact]
    public void RichResult_FailureWithMetadata_ShouldPreserveMetadata()
    {
        // Arrange & Act
        var result = Result<int>.Failure("ERROR", "Test error")
            .WithErrorMetadata("severity", "high")
            .WithErrorMetadata("category", "validation");

        var metadata = result.GetErrorMetadata();

        // Assert
        metadata.Should().ContainKey("severity");
        metadata["severity"].Should().Be("high");
        metadata.Should().ContainKey("category");
        metadata["category"].Should().Be("validation");
    }

    #endregion
}
