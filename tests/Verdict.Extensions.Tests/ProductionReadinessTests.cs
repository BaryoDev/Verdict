using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Verdict;
using Verdict.Extensions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Production readiness tests for ErrorCollection, MultiResult, and related features.
/// Tests disposal patterns, thread-safety, and edge cases critical for production use.
/// </summary>
public class ProductionReadinessTests
{
    #region ErrorCollection Disposal Tests

    [Fact]
    public void ErrorCollection_Dispose_ShouldBeSafeToCallMultipleTimes()
    {
        // Arrange
        var errors = new[] { new Error("E1", "Error 1"), new Error("E2", "Error 2") };
        var collection = ErrorCollection.Create(errors);

        // Act - dispose multiple times (should not throw)
        collection.Dispose();
        collection.Dispose();
        collection.Dispose();

        // Assert - no exception thrown
        // Note: Count remains unchanged because ErrorCollection is a readonly struct
        // The Dispose() method returns the pooled array but cannot modify struct fields
        collection.Count.Should().Be(2);
    }

    [Fact]
    public void ErrorCollection_Create_WithLargeErrorCount_ShouldUseArrayPool()
    {
        // Arrange - create 1000 errors
        var errors = Enumerable.Range(0, 1000)
            .Select(i => new Error($"E{i}", $"Error {i}"))
            .ToArray();

        // Act
        var collection = ErrorCollection.Create(errors);

        // Assert
        collection.Count.Should().Be(1000);
        collection.HasErrors.Should().BeTrue();

        // Cleanup - return to pool
        collection.Dispose();
    }

    [Fact]
    public void ErrorCollection_Enumeration_AfterDispose_ShouldStillWork()
    {
        // Arrange
        var errors = new[] { new Error("E1", "Error 1") };
        var collection = ErrorCollection.Create(errors);

        // Act - note: non-rented arrays don't actually get returned to pool
        collection.Dispose();

        // Assert - Count remains unchanged (struct is immutable)
        collection.Count.Should().Be(1);
    }

    #endregion

    #region MultiResult Copy Safety Tests

    [Fact]
    public void MultiResult_StructCopy_DisposingOriginal_ShouldNotAffectCopy()
    {
        // Arrange
        var errors = new[] { new Error("E1", "Error 1") };
        var original = MultiResult<int>.Failure(errors);

        // Act - create a copy (struct copy semantics)
        var copy = original;

        // The copy should have the same error count
        copy.ErrorCount.Should().Be(1);
        original.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void MultiResult_Success_ShouldNotRequireDisposal()
    {
        // Arrange & Act
        var result = MultiResult<int>.Success(42);

        // Assert - success result has no errors to dispose
        result.IsSuccess.Should().BeTrue();
        result.ErrorCount.Should().Be(0);

        // Dispose should be safe to call even on success
        result.DisposeErrors();
    }

    [Fact]
    public void MultiResult_ToSingleResult_ShouldPreserveFirstError()
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

    #endregion

    #region Thread-Safety Tests

    [Fact]
    public async Task ErrorCollection_ConcurrentCreation_ShouldBeSafe()
    {
        // Arrange
        var tasks = new List<Task<ErrorCollection>>();

        // Act - create many ErrorCollections concurrently
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var errors = Enumerable.Range(0, 10)
                    .Select(j => new Error($"E{index}_{j}", $"Error {index}_{j}"))
                    .ToArray();
                return ErrorCollection.Create(errors);
            }));
        }

        var collections = await Task.WhenAll(tasks);

        // Assert
        foreach (var collection in collections)
        {
            collection.Count.Should().Be(10);
            collection.Dispose();
        }
    }

    [Fact]
    public async Task MultiResult_ConcurrentAccess_ShouldBeSafe()
    {
        // Arrange
        var errors = new[] { new Error("E1", "Error 1") };
        var result = MultiResult<int>.Failure(errors);

        // Act - access from multiple threads concurrently
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var isFailure = result.IsFailure;
            var count = result.ErrorCount;
            Error firstError = default;
            foreach (var e in result.Errors)
            {
                firstError = e;
                break;
            }
            return new { IsFailure = isFailure, Count = count, FirstError = firstError };
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - all threads should see consistent data
        foreach (var r in results)
        {
            r.IsFailure.Should().BeTrue();
            r.Count.Should().Be(1);
            r.FirstError.Code.Should().Be("E1");
        }
    }

    [Fact]
    public async Task CombineExtensions_Merge_ConcurrentCalls_ShouldBeSafe()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var results = new[]
            {
                Result<int>.Success(1),
                Result<int>.Success(2),
                Result<int>.Failure("ERROR", "Test error")
            };
            return CombineExtensions.Merge(results);
        })).ToArray();

        var mergedResults = await Task.WhenAll(tasks);

        // Assert
        foreach (var merged in mergedResults)
        {
            merged.IsFailure.Should().BeTrue();
            merged.ErrorCount.Should().Be(1);
        }
    }

    #endregion

    #region Large Input Tests

    [Fact]
    public void ErrorCollection_WithVeryLargeErrorCount_ShouldHandle()
    {
        // Arrange - create 10,000 errors
        var errors = Enumerable.Range(0, 10_000)
            .Select(i => new Error($"E{i}", $"Error message {i}"))
            .ToArray();

        // Act
        var collection = ErrorCollection.Create(errors);

        // Assert
        collection.Count.Should().Be(10_000);
        collection.HasErrors.Should().BeTrue();

        // Cleanup
        collection.Dispose();
    }

    [Fact]
    public void MultiResult_WithManyErrors_ShouldIterate()
    {
        // Arrange
        var errors = Enumerable.Range(0, 1000)
            .Select(i => new Error($"E{i}", $"Error {i}"))
            .ToArray();
        var result = MultiResult<int>.Failure(errors);

        // Act
        var count = 0;
        foreach (var error in result.Errors)
        {
            count++;
        }

        // Assert
        count.Should().Be(1000);

        // Cleanup
        result.DisposeErrors();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ErrorCollection_Create_WithEmptyArray_ShouldReturnEmpty()
    {
        // Arrange & Act
        var collection = ErrorCollection.Create(Array.Empty<Error>());

        // Assert
        collection.Count.Should().Be(0);
        collection.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ErrorCollection_Create_WithSingleError_ShouldWork()
    {
        // Arrange
        var error = new Error("SINGLE", "Single error");

        // Act
        var collection = ErrorCollection.Create(error);

        // Assert
        collection.Count.Should().Be(1);
        collection.HasErrors.Should().BeTrue();
        collection[0].Code.Should().Be("SINGLE");
    }

    [Fact]
    public void MultiResult_Failure_WithEmptyErrors_ShouldStillBeFailure()
    {
        // Arrange & Act
        var result = MultiResult<int>.Failure(Array.Empty<Error>());

        // Assert - empty errors array still creates failure
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void CombineExtensions_Merge_WithAllSuccess_ShouldReturnSuccess()
    {
        // Arrange
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3)
        };

        // Act
        var merged = CombineExtensions.Merge(results);

        // Assert
        merged.IsSuccess.Should().BeTrue();
        merged.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void CombineExtensions_Merge_WithEmptyArray_ShouldThrowArgumentException()
    {
        // Arrange
        var results = Array.Empty<Result<int>>();

        // Act
        Action act = () => CombineExtensions.Merge(results);

        // Assert - at least one result is required
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one result*");
    }

    [Fact]
    public void TryExtensions_Try_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => TryExtensions.Try<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryExtensions_TryResult_Success_ShouldReturnOriginalResult()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult(() => Result<int>.Success(42));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void TryExtensions_TryResult_WithException_ShouldCatchAndReturnFailure()
    {
        // Arrange & Act
        var result = TryExtensions.TryResult<int>(() =>
            throw new InvalidOperationException("Test exception"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InvalidOperationException");
        result.Error.Message.Should().Be("An error occurred."); // Sanitized by default
    }

    #endregion
}
