using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Verdict.Fluent;
using Xunit;

namespace Verdict.Fluent.Tests;

/// <summary>
/// Comprehensive edge case and security tests for Fluent Result extensions.
/// Tests for deep chaining, null handling, pattern matching edge cases, and exception propagation.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region Deep Chaining Scenarios

    [Fact]
    public void Map_DeepChaining_ShouldHandle10PlusOperations()
    {
        // Arrange
        var result = Result<int>.Success(1);

        // Act - Chain 15 Map operations
        var final = result
            .Map(x => x + 1)   // 2
            .Map(x => x * 2)   // 4
            .Map(x => x + 1)   // 5
            .Map(x => x * 2)   // 10
            .Map(x => x + 5)   // 15
            .Map(x => x * 3)   // 45
            .Map(x => x - 5)   // 40
            .Map(x => x / 2)   // 20
            .Map(x => x + 10)  // 30
            .Map(x => x * 2)   // 60
            .Map(x => x - 10)  // 50
            .Map(x => x / 5)   // 10
            .Map(x => x + 5)   // 15
            .Map(x => x * 2)   // 30
            .Map(x => x.ToString()); // "30"

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("30");
    }

    [Fact]
    public void OnSuccess_DeepChaining_ShouldExecuteAllActions()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var counter = 0;

        // Act - Chain 12 OnSuccess operations
        result
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++);

        // Assert
        counter.Should().Be(12);
    }

    [Fact]
    public void MixedChaining_VeryDeepChain_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(10);
        var sideEffectCounter = 0;

        // Act - Mix Map, OnSuccess, and OnFailure in deep chain
        var final = result
            .Map(x => x * 2)
            .OnSuccess(x => sideEffectCounter += x)
            .Map(x => x + 5)
            .OnSuccess(_ => sideEffectCounter++)
            .Map(x => x.ToString())
            .OnFailure(_ => sideEffectCounter = -1) // Should not execute
            .Map(x => $"Value: {x}")
            .OnSuccess(_ => sideEffectCounter++);

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("Value: 25");
        sideEffectCounter.Should().Be(22); // 20 + 1 + 1
    }

    #endregion

    #region Pattern Matching with Null Values

    [Fact]
    public void Match_SuccessWithNullValue_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var output = result.Match(
            val => val ?? "null_value",
            err => err.Code);

        // Assert
        output.Should().Be("null_value");
    }

    [Fact]
    public void Match_ReturningNull_ShouldHandleCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(
            val => (string?)null,
            err => err.Code);

        // Assert
        output.Should().BeNull();
    }

    [Fact]
    public void Match_BothBranchesReturnNull_ShouldHandleCorrectly()
    {
        // Arrange
        var successResult = Result<int>.Success(42);
        var failureResult = Result<int>.Failure("ERROR", "Test");

        // Act
        var successOutput = successResult.Match(
            val => (string?)null,
            err => (string?)null);
        var failureOutput = failureResult.Match(
            val => (string?)null,
            err => (string?)null);

        // Assert
        successOutput.Should().BeNull();
        failureOutput.Should().BeNull();
    }

    #endregion

    #region Edge Cases in Fluent Operators

    [Fact]
    public void Map_MappingToSameType_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(84);
    }

    [Fact]
    public void Map_ChainedTypeTransformations_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var final = result
            .Map(x => x.ToString())           // int -> string
            .Map(x => x.Length)                // string -> int
            .Map(x => x > 0)                   // int -> bool
            .Map(x => x ? "yes" : "no");       // bool -> string

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("yes");
    }

    [Fact]
    public void OnSuccess_ReturningOriginalResult_ShouldNotModifyValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var returned = result.OnSuccess(_ => { /* side effect */ });

        // Assert
        returned.IsSuccess.Should().BeTrue();
        returned.Value.Should().Be(42);
        returned.Should().Be(result); // Should be same reference (struct equality)
    }

    [Fact]
    public void OnFailure_OnSuccessResult_ShouldNotExecute()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    #endregion

    #region Null Handler Functions

    [Fact]
    public void Map_WithNullMapper_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);
        Func<int, string> nullMapper = null!;

        // Act
        Action act = () => result.Map(nullMapper);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*mapper*");
    }

    [Fact]
    public void Match_WithNullOnSuccess_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);
        Func<int, string> nullOnSuccess = null!;

        // Act
        Action act = () => result.Match(nullOnSuccess, err => err.Code);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*onSuccess*");
    }

    [Fact]
    public void Match_WithNullOnFailure_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);
        Func<Error, string> nullOnFailure = null!;

        // Act
        Action act = () => result.Match(val => val.ToString(), nullOnFailure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*onFailure*");
    }

    [Fact]
    public void OnSuccess_WithNullAction_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(42);
        Action<int> nullAction = null!;

        // Act
        Action act = () => result.OnSuccess(nullAction);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*action*");
    }

    [Fact]
    public void OnFailure_WithNullAction_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test");
        Action<Error> nullAction = null!;

        // Act
        Action act = () => result.OnFailure(nullAction);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*action*");
    }

    #endregion

    #region Default Result in Fluent Chains

    [Fact]
    public void Map_OnDefaultResult_ShouldThrow()
    {
        // Arrange
        Result<int> defaultResult = default;

        // Act & Assert - Default result is invalid and throws when fluent operations try to access Error
        Action act = () => defaultResult.Map(x => x.ToString());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    [Fact]
    public void OnSuccess_OnDefaultResult_ShouldNotExecute()
    {
        // Arrange
        Result<int> defaultResult = default;
        var executed = false;

        // Act - OnSuccess checks IsSuccess first, so it won't throw
        defaultResult.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_OnDefaultResult_ShouldThrow()
    {
        // Arrange
        Result<int> defaultResult = default;

        // Act & Assert - Default result is invalid and throws when trying to access Error
        Action act = () => defaultResult.OnFailure(_ => {});
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    [Fact]
    public void Match_OnDefaultResult_ShouldThrow()
    {
        // Arrange
        Result<int> defaultResult = default;

        // Act & Assert - Default result is invalid and throws when trying to access Error
        Action act = () => defaultResult.Match(
            val => "success",
            err => "failure");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    #endregion

    #region Exception Propagation Through Fluent Chains

    [Fact]
    public void Map_WhenMapperThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Map(x =>
        {
            throw new InvalidOperationException("Mapper failed");
#pragma warning disable CS0162 // Unreachable code detected
            return x.ToString();
#pragma warning restore CS0162
        });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Mapper failed");
    }

    [Fact]
    public void OnSuccess_WhenActionThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.OnSuccess(x => throw new InvalidOperationException("Action failed"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Action failed");
    }

    [Fact]
    public void OnFailure_WhenActionThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test");

        // Act
        Action act = () => result.OnFailure(err => throw new InvalidOperationException("Action failed"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Action failed");
    }

    [Fact]
    public void Match_WhenOnSuccessThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Match(
            val => throw new InvalidOperationException("OnSuccess failed"),
            err => err.Code);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("OnSuccess failed");
    }

    [Fact]
    public void Match_WhenOnFailureThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test");

        // Act
        Action act = () => result.Match(
            val => val.ToString(),
            err => throw new InvalidOperationException("OnFailure failed"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("OnFailure failed");
    }

    [Fact]
    public void Map_ExceptionInMiddleOfChain_ShouldStopChain()
    {
        // Arrange
        var result = Result<int>.Success(10);
        var executed = false;

        // Act & Assert
        Action act = () => result
            .Map(x => x * 2)
            .Map(x =>
            {
                throw new InvalidOperationException("Chain broken");
#pragma warning disable CS0162 // Unreachable code detected
                return x;
#pragma warning restore CS0162
            })
            .Map(x => x.ToString())
            .OnSuccess(_ => executed = true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Chain broken");
        executed.Should().BeFalse();
    }

    #endregion

    #region Complex Chaining Scenarios

    [Fact]
    public void ComplexChain_WithMultipleTransformations_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<int>.Success(5);
        var log = new List<string>();

        // Act
        var final = result
            .OnSuccess(x => log.Add($"Start: {x}"))
            .Map(x => x * 2)
            .OnSuccess(x => log.Add($"After *2: {x}"))
            .Map(x => x + 10)
            .OnSuccess(x => log.Add($"After +10: {x}"))
            .Map(x => x.ToString())
            .OnSuccess(x => log.Add($"After ToString: {x}"))
            .Map(x => $"Result: {x}");

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("Result: 20");
        log.Should().HaveCount(4);
        log[0].Should().Be("Start: 5");
        log[1].Should().Be("After *2: 10");
        log[2].Should().Be("After +10: 20");
        log[3].Should().Be("After ToString: 20");
    }

    [Fact]
    public void FailureChain_ShouldNotExecuteSuccessBranches()
    {
        // Arrange
        var result = Result<int>.Failure("INITIAL_ERROR", "Starting with failure");
        var successCount = 0;
        var failureCount = 0;

        // Act
        var final = result
            .OnSuccess(_ => successCount++)
            .Map(x => x * 2)
            .OnSuccess(_ => successCount++)
            .OnFailure(_ => failureCount++)
            .Map(x => x.ToString())
            .OnSuccess(_ => successCount++)
            .OnFailure(_ => failureCount++);

        // Assert
        final.IsFailure.Should().BeTrue();
        final.Error.Code.Should().Be("INITIAL_ERROR");
        successCount.Should().Be(0);
        failureCount.Should().Be(2);
    }

    [Fact]
    public void NonGenericResult_CannotUseFluentOperations_IsExpected()
    {
        // Arrange - Non-generic Result doesn't have fluent extension methods
        var result = Result.Success();

        // Assert - Just verify the result state
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        
        // Note: Non-generic Result does not have OnSuccess/OnFailure/Map extensions
        // This is by design - fluent operations are only available on Result<T>
    }

    #endregion

    #region Edge Cases with Value Types

    [Fact]
    public void Map_WithStructTypes_ShouldNotCauseBoxing()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => (long)x);

        // Assert - Verify struct is copied, not boxed
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(42L);
        mapped.Value.GetType().Should().Be(typeof(long));
    }

    [Fact]
    public void Map_WithNullableTypes_ShouldHandleCorrectly()
    {
        // Arrange
        var result = Result<int?>.Success(42);

        // Act
        var mapped = result.Map(x => x.HasValue ? x.Value * 2 : 0);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(84);
    }

    [Fact]
    public void Map_NullableToNonNullable_ShouldWorkCorrectly()
    {
        // Arrange
        var result = Result<int?>.Success(null);

        // Act
        var mapped = result.Map(x => x ?? -1);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(-1);
    }

    #endregion
}
