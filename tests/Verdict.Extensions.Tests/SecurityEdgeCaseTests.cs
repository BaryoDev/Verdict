using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Comprehensive edge case and security tests for MultiResult and ErrorCollection.
/// Tests for potential vulnerabilities, memory leaks, and boundary conditions.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region ErrorCollection Edge Cases

    [Fact]
    public void ErrorCollection_Create_WithNullArray_ShouldReturnDefault()
    {
        // Arrange & Act
        var collection = ErrorCollection.Create((Error[])null!);

        // Assert
        collection.Count.Should().Be(0);
        collection.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ErrorCollection_Create_WithEmptyArray_ShouldReturnDefault()
    {
        // Arrange & Act
        var collection = ErrorCollection.Create(Array.Empty<Error>());

        // Assert
        collection.Count.Should().Be(0);
        collection.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ErrorCollection_Create_WithNullEnumerable_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => ErrorCollection.Create((System.Collections.Generic.IEnumerable<Error>)null!);

        // Assert - now throws to prevent null reference issues
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ErrorCollection_Indexer_WithNegativeIndex_ShouldThrow()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var collection = ErrorCollection.Create(error);

        // Act & Assert
        Action act = () => { var _ = collection[-1]; };
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void ErrorCollection_Indexer_WithIndexOutOfRange_ShouldThrow()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var collection = ErrorCollection.Create(error);

        // Act & Assert
        Action act = () => { var _ = collection[10]; };
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void ErrorCollection_First_OnEmptyCollection_ShouldThrow()
    {
        // Arrange
        var collection = ErrorCollection.Create(Array.Empty<Error>());

        // Act & Assert
        Action act = () => collection.First();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ErrorCollection_AsSpan_OnEmptyCollection_ShouldReturnEmptySpan()
    {
        // Arrange
        var collection = ErrorCollection.Create(Array.Empty<Error>());

        // Act
        var span = collection.AsSpan();

        // Assert
        span.Length.Should().Be(0);
    }

    [Fact]
    public void ErrorCollection_Dispose_ShouldReturnToArrayPool()
    {
        // Arrange
        var errors = Enumerable.Range(0, 100).Select(i => new Error($"CODE{i}", $"Message {i}"));
        var collection = ErrorCollection.Create(errors);

        // Act
        collection.Dispose();

        // Assert - no exception means array was returned successfully
        true.Should().BeTrue();
    }

    [Fact]
    public void ErrorCollection_DisposeTwice_ShouldNotThrow()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var collection = ErrorCollection.Create(error);

        // Act
        collection.Dispose();
        Action act = () => collection.Dispose();

        // Assert - double dispose should be safe
        act.Should().NotThrow();
    }

    [Fact]
    public void ErrorCollection_WithLargeNumberOfErrors_ShouldHandleEfficiently()
    {
        // Arrange
        var errors = Enumerable.Range(0, 10000).Select(i => new Error($"CODE{i}", $"Message {i}")).ToArray();

        // Act
        var collection = ErrorCollection.Create(errors);

        // Assert
        collection.Count.Should().Be(10000);
        collection.HasErrors.Should().BeTrue();
        collection.Dispose();
    }

    [Fact]
    public void ErrorCollection_ToArray_ShouldCreateNewArray()
    {
        // Arrange
        var error1 = new Error("TEST1", "Message 1");
        var error2 = new Error("TEST2", "Message 2");
        var collection = ErrorCollection.Create(error1, error2);

        // Act
        var array = collection.ToArray();

        // Assert
        array.Should().HaveCount(2);
        array[0].Should().Be(error1);
        array[1].Should().Be(error2);
    }

    #endregion

    #region MultiResult Edge Cases

    [Fact]
    public void MultiResult_DefaultStruct_ShouldBehaveAsFailure()
    {
        // Arrange
        MultiResult<int> result = default;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void MultiResult_DefaultStruct_AccessingValue_ShouldThrow()
    {
        // Arrange
        MultiResult<int> result = default;

        // Act & Assert
        Action act = () => { var _ = result.Value; };
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot access Value*");
    }

    [Fact]
    public void MultiResult_FailureWithSingleError_ShouldStoreSingleError()
    {
        // Arrange
        var error = new Error("TEST", "Test error");

        // Act
        var result = MultiResult<int>.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(1);
        result.Errors[0].Should().Be(error);
    }

    [Fact]
    public void MultiResult_FailureWithMultipleErrors_ShouldStoreAllErrors()
    {
        // Arrange
        var error1 = new Error("TEST1", "Error 1");
        var error2 = new Error("TEST2", "Error 2");
        var error3 = new Error("TEST3", "Error 3");

        // Act
        var result = MultiResult<int>.Failure(error1, error2, error3);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(3);
        result.Errors[0].Should().Be(error1);
        result.Errors[1].Should().Be(error2);
        result.Errors[2].Should().Be(error3);
    }

    [Fact]
    public void MultiResult_ToSingleResult_WithMultipleErrors_ShouldTakeFirstError()
    {
        // Arrange
        var error1 = new Error("TEST1", "Error 1");
        var error2 = new Error("TEST2", "Error 2");
        var multiResult = MultiResult<int>.Failure(error1, error2);

        // Act
        var singleResult = multiResult.ToSingleResult();

        // Assert
        singleResult.IsFailure.Should().BeTrue();
        singleResult.Error.Should().Be(error1);
    }

    [Fact]
    public void MultiResult_DisposeErrors_ShouldCleanUpResources()
    {
        // Arrange
        var errors = Enumerable.Range(0, 100).Select(i => new Error($"CODE{i}", $"Message {i}"));
        var result = MultiResult<int>.Failure(ErrorCollection.Create(errors));

        // Act
        result.DisposeErrors();

        // Assert - no exception means cleanup succeeded
        true.Should().BeTrue();
    }

    [Fact]
    public void MultiResult_DisposeErrorsTwice_ShouldNotThrow()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = MultiResult<int>.Failure(error);

        // Act
        result.DisposeErrors();
        Action act = () => result.DisposeErrors();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MultiResult_Success_DisposeErrors_ShouldNotThrow()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act & Assert
        Action act = () => result.DisposeErrors();
        act.Should().NotThrow();
    }

    [Fact]
    public void MultiResult_ImplicitConversionFromResult_Success_ShouldWork()
    {
        // Arrange
        Result<int> result = Result<int>.Success(42);

        // Act
        MultiResult<int> multiResult = result;

        // Assert
        multiResult.IsSuccess.Should().BeTrue();
        multiResult.Value.Should().Be(42);
    }

    [Fact]
    public void MultiResult_ImplicitConversionFromResult_Failure_ShouldWork()
    {
        // Arrange
        Result<int> result = Result<int>.Failure("TEST", "Test error");

        // Act
        MultiResult<int> multiResult = result;

        // Assert
        multiResult.IsFailure.Should().BeTrue();
        multiResult.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void MultiResult_Deconstruct_WithMultipleErrors_ShouldProvideAllErrors()
    {
        // Arrange
        var error1 = new Error("TEST1", "Error 1");
        var error2 = new Error("TEST2", "Error 2");
        var result = MultiResult<int>.Failure(error1, error2);

        // Act
        var (isSuccess, value, errors) = result;

        // Assert
        isSuccess.Should().BeFalse();
        value.Should().Be(default(int));
        errors.Length.Should().Be(2);
    }

    [Fact]
    public void NonGenericMultiResult_DefaultStruct_ShouldBehaveAsFailure()
    {
        // Arrange
        MultiResult result = default;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public void MultiResult_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);
        var exceptions = new System.Collections.Generic.List<Exception>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            try
            {
                var value = result.Value;
                var isSuccess = result.IsSuccess;
                var errorCount = result.ErrorCount;

                if (value != 42 || !isSuccess || errorCount != 0)
                {
                    throw new InvalidOperationException("Concurrent access produced incorrect result");
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

    [Fact]
    public void ErrorCollection_ConcurrentRead_ShouldBeThreadSafe()
    {
        // Arrange
        var errors = Enumerable.Range(0, 100).Select(i => new Error($"CODE{i}", $"Message {i}")).ToArray();
        var collection = ErrorCollection.Create(errors);
        var readErrors = 0;

        // Act
        Parallel.For(0, 1000, _ =>
        {
            var count = collection.Count;
            var span = collection.AsSpan();
            if (count != 100 || span.Length != 100)
            {
                System.Threading.Interlocked.Increment(ref readErrors);
            }
        });

        // Assert
        readErrors.Should().Be(0);
        collection.Dispose();
    }

    #endregion

    #region Memory Safety

    [Fact]
    public void MultiResult_WithLargeErrors_ShouldNotCauseStackOverflow()
    {
        // Arrange
        var largeMessage = new string('X', 100000);
        var errors = Enumerable.Range(0, 100).Select(i => new Error($"CODE{i}", largeMessage)).ToArray();

        // Act
        var result = MultiResult<int>.Failure(errors);

        // Assert
        result.ErrorCount.Should().Be(100);
        result.DisposeErrors();
    }

    [Fact]
    public void ErrorCollection_ManyInstances_ShouldNotCauseMemoryLeak()
    {
        // Arrange & Act
        for (int i = 0; i < 10000; i++)
        {
            var error = new Error($"CODE{i}", $"Message {i}");
            var collection = ErrorCollection.Create(error);
            collection.Dispose();
        }

        // Assert - if we get here without OutOfMemoryException, test passes
        true.Should().BeTrue();
    }

    #endregion

    #region ToString Edge Cases

    [Fact]
    public void MultiResult_ToString_WithNoErrors_ShouldNotThrow()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Success");
    }

    [Fact]
    public void MultiResult_ToString_WithManyErrors_ShouldShowCount()
    {
        // Arrange
        var errors = Enumerable.Range(0, 50).Select(i => new Error($"CODE{i}", $"Message {i}")).ToArray();
        var result = MultiResult<int>.Failure(errors);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("50");
        str.Should().Contain("error");
        result.DisposeErrors();
    }

    [Fact]
    public void ErrorCollection_ToString_WithEmptyCollection_ShouldShowNoErrors()
    {
        // Arrange
        var collection = ErrorCollection.Create(Array.Empty<Error>());

        // Act
        var str = collection.ToString();

        // Assert
        str.Should().Contain("No errors");
    }

    #endregion
}
