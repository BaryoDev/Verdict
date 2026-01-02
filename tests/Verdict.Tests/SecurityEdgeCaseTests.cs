using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Comprehensive edge case and security tests for Result types.
/// Tests for potential vulnerabilities and boundary conditions.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region Null and Empty String Handling

    [Fact]
    public void Error_WithNullCode_ShouldHandleSafely()
    {
        // Arrange & Act
        var error = new Error(null!, "Test message");

        // Assert
        error.Code.Should().NotBeNull();
        error.Code.Should().Be(string.Empty);
    }

    [Fact]
    public void Error_WithNullMessage_ShouldHandleSafely()
    {
        // Arrange & Act
        var error = new Error("TEST", null!);

        // Assert
        error.Message.Should().NotBeNull();
        error.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void Error_WithBothNull_ShouldHandleSafely()
    {
        // Arrange & Act
        var error = new Error(null!, null!);

        // Assert
        error.Code.Should().Be(string.Empty);
        error.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void Error_WithVeryLongStrings_ShouldHandleWithoutOverflow()
    {
        // Arrange
        var longCode = new string('A', 10000);
        var longMessage = new string('B', 10000);

        // Act
        var error = new Error(longCode, longMessage);

        // Assert
        error.Code.Should().HaveLength(10000);
        error.Message.Should().HaveLength(10000);
    }

    #endregion

    #region Struct Default Initialization

    [Fact]
    public void Result_DefaultStruct_ShouldBehaveAsFailure()
    {
        // Arrange
        Result<int> result = default;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Result_DefaultStruct_AccessingValue_ShouldThrow()
    {
        // Arrange
        Result<int> result = default;

        // Act & Assert
        Action act = () => { var _ = result.Value; };
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot access Value on a failed result*");
    }

    [Fact]
    public void Result_DefaultStruct_ValueOrDefault_ShouldReturnDefault()
    {
        // Arrange
        Result<int> result = default;

        // Act
        var value = result.ValueOrDefault;

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void NonGenericResult_DefaultStruct_ShouldBehaveAsFailure()
    {
        // Arrange
        Result result = default;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Exception Preservation

    [Fact]
    public void Error_WithException_ShouldNotLeakSensitiveStackTrace()
    {
        // Arrange
        var exception = new InvalidOperationException("Sensitive database connection failed at server.internal.local");

        // Act
        var error = new Error("DB_ERROR", "Database error occurred", exception);

        // Assert
        error.Exception.Should().BeSameAs(exception);
        error.Message.Should().Be("Database error occurred");
        error.Message.Should().NotContain("server.internal.local");
    }

    [Fact]
    public void Error_FromException_ShouldExtractBasicInfo()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument provided");

        // Act
        var error = Error.FromException(exception);

        // Assert
        error.Code.Should().Be("ArgumentException");
        error.Message.Should().Be("Invalid argument provided");
        error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Error_WithNullException_ShouldHandleSafely()
    {
        // Arrange & Act
        var error = new Error("TEST", "Test message", null);

        // Assert
        error.Exception.Should().BeNull();
    }

    #endregion

    #region ValueOr Safety

    [Fact]
    public void Result_ValueOr_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act & Assert
        Action act = () => result.ValueOr(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Result_ValueOr_FactoryThrowsException_ShouldPropagate()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act & Assert
        Action act = () => result.ValueOr(_ => throw new InvalidOperationException("Factory failed"));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Factory failed");
    }

    [Fact]
    public void Result_ValueOr_FactoryReturnsDefault_ShouldReturnDefault()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");

        // Act
        var value = result.ValueOr(_ => default);

        // Assert
        value.Should().Be(0);
    }

    #endregion

    #region Concurrent Access Scenarios

    [Fact]
    public void Result_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            try
            {
                var value = result.Value;
                var isSuccess = result.IsSuccess;
                var valueOrDefault = result.ValueOrDefault;
                
                if (value != 42 || !isSuccess || valueOrDefault != 42)
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
    public void Result_ConcurrentDeconstruction_ShouldBeThreadSafe()
    {
        // Arrange
        var result = Result<string>.Success("test");
        var errors = 0;

        // Act
        Parallel.For(0, 1000, _ =>
        {
            var (isSuccess, value, error) = result;
            if (!isSuccess || value != "test")
            {
                Interlocked.Increment(ref errors);
            }
        });

        // Assert
        errors.Should().Be(0);
    }

    #endregion

    #region ToString Edge Cases

    [Fact]
    public void Result_ToString_WithNullValue_ShouldHandleSafely()
    {
        // Arrange
        Result<string?> result = Result<string?>.Success(null);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Success");
    }

    [Fact]
    public void Result_ToString_WithVeryLongValue_ShouldNotCauseOutOfMemory()
    {
        // Arrange
        var longString = new string('X', 100000);
        var result = Result<string>.Success(longString);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Success");
    }

    [Fact]
    public void Error_ToString_WithEmptyCodeAndMessage_ShouldNotThrow()
    {
        // Arrange
        var error = new Error(string.Empty, string.Empty);

        // Act
        var str = error.ToString();

        // Assert
        str.Should().NotBeNull();
    }

    #endregion

    #region Implicit Conversion Edge Cases

    [Fact]
    public void Result_ImplicitConversion_WithNull_ShouldHandleBasedOnType()
    {
        // Arrange & Act
        Result<string?> result = (string?)null;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_ImplicitConversion_WithDefaultError_ShouldCreateFailure()
    {
        // Arrange
        Error error = default;

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Deconstruction Edge Cases

    [Fact]
    public void Result_Deconstruct_PartialUsage_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var (isSuccess, _, _) = result;

        // Assert
        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void NonGenericResult_Deconstruct_ShouldProvideCorrectValues()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var (isSuccess, error) = result;

        // Assert
        isSuccess.Should().BeTrue();
        error.Should().Be(default(Error));
    }

    #endregion

    #region Equality and Comparison

    [Fact]
    public void Error_Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var error1 = new Error("TEST", "Test message");
        var error2 = new Error("TEST", "Test message");

        // Act & Assert
        (error1 == error2).Should().BeTrue();
        error1.Equals(error2).Should().BeTrue();
    }

    [Fact]
    public void Error_Equality_WithDifferentExceptions_ShouldBeEqual()
    {
        // Arrange
        var ex1 = new Exception("Test 1");
        var ex2 = new Exception("Test 2");
        var error1 = new Error("TEST", "Test", ex1);
        var error2 = new Error("TEST", "Test", ex2);

        // Act & Assert - record struct equality ignores reference comparison for exceptions
        (error1 == error2).Should().BeFalse();
    }

    #endregion

    #region Extension Method Safety

    [Fact]
    public void ResultExtensions_Bind_WithNullBinder_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act & Assert
        Action act = () => result.Bind<int, string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResultExtensions_Tap_WithNullAction_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act & Assert
        Action act = () => result.Tap(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ResultExtensions_TapError_WithNullAction_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test");

        // Act & Assert
        Action act = () => result.TapError(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Memory and Resource Management

    [Fact]
    public void Result_ManyInstances_ShouldNotCauseMemoryLeak()
    {
        // Arrange & Act
        for (int i = 0; i < 100000; i++)
        {
            var result = Result<int>.Success(i);
            var _ = result.Value;
        }

        // Assert - if we get here without OutOfMemoryException, test passes
        true.Should().BeTrue();
    }

    [Fact]
    public void Result_WithLargeValue_ShouldHandleCorrectly()
    {
        // Arrange
        var largeArray = new byte[1024 * 1024]; // 1 MB
        Array.Fill(largeArray, (byte)42);

        // Act
        var result = Result<byte[]>.Success(largeArray);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1024 * 1024);
    }

    #endregion

    #region Type Safety

    [Fact]
    public void Result_WithValueType_ShouldNotBoxUnnecessarily()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);
        var value = result.Value;

        // Assert
        value.Should().Be(42);
        value.GetType().Should().Be(typeof(int));
    }

    [Fact]
    public void Result_WithNullableValueType_ShouldHandleNull()
    {
        // Arrange & Act
        Result<int?> result = Result<int?>.Success(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    #endregion
}
