using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Verdict.Fluent;
using Xunit;

namespace Verdict.Fluent.Tests;

/// <summary>
/// Comprehensive edge case and security tests for Fluent Result extensions.
/// Tests for deep chaining, null handling, exception propagation, and type transformations.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region Deep Chaining Tests

    [Fact]
    public void Map_DeepChaining_ShouldMaintainValue()
    {
        // Arrange
        var result = Result<int>.Success(1);

        // Act - Chain 15+ Map operations
        var chained = result
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1)
            .Map(x => x + 1);

        // Assert
        chained.IsSuccess.Should().BeTrue();
        chained.Value.Should().Be(16);
    }

    [Fact]
    public void OnSuccess_DeepChaining_ShouldExecuteAllActions()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var counter = 0;

        // Act - Chain 15+ OnSuccess operations
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
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++);

        // Assert
        counter.Should().Be(15);
    }

    [Fact]
    public void MixedChain_DeepNesting_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Success(1);
        var sideEffects = new List<string>();

        // Act
        var final = result
            .Map(x => x + 1)
            .OnSuccess(x => sideEffects.Add($"After first map: {x}"))
            .Map(x => x * 2)
            .OnSuccess(x => sideEffects.Add($"After multiply: {x}"))
            .Map(x => x.ToString())
            .OnSuccess(x => sideEffects.Add($"After toString: {x}"))
            .Map(x => int.Parse(x))
            .OnSuccess(x => sideEffects.Add($"After parse: {x}"))
            .Map(x => x + 100)
            .OnSuccess(x => sideEffects.Add($"Final: {x}"));

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be(104);
        sideEffects.Should().HaveCount(5);
    }

    [Fact]
    public void DeepChain_FailureShortCircuits_ShouldNotExecuteAfterFailure()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Initial failure");
        var counter = 0;

        // Act
        var final = result
            .Map(x => { counter++; return x + 1; })
            .Map(x => { counter++; return x + 1; })
            .Map(x => { counter++; return x + 1; })
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++)
            .OnSuccess(_ => counter++);

        // Assert
        final.IsFailure.Should().BeTrue();
        counter.Should().Be(0);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Match_WithNullableValue_Success_ShouldReturnValue()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var output = result.Match(
            val => val ?? "was null",
            err => err.Code);

        // Assert
        output.Should().Be("was null");
    }

    [Fact]
    public void Match_OnSuccessReturnsNull_ShouldReturnNull()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match<int, string?>(
            _ => null,
            err => err.Code);

        // Assert
        output.Should().BeNull();
    }

    [Fact]
    public void Match_OnFailureReturnsNull_ShouldReturnNull()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");

        // Act
        var output = result.Match<int, string?>(
            val => val.ToString(),
            _ => null);

        // Assert
        output.Should().BeNull();
    }

    [Fact]
    public void Match_BothBranchesReturnSameValue_ShouldWork()
    {
        // Arrange
        var successResult = Result<int>.Success(42);
        var failureResult = Result<int>.Failure("ERR", "Msg");

        // Act
        var successOutput = successResult.Match(_ => "constant", _ => "constant");
        var failureOutput = failureResult.Match(_ => "constant", _ => "constant");

        // Assert
        successOutput.Should().Be("constant");
        failureOutput.Should().Be("constant");
    }

    #endregion

    #region Null Handler Tests

    [Fact]
    public void Match_NullOnSuccess_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Match(null!, err => err.Code);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("onSuccess");
    }

    [Fact]
    public void Match_NullOnFailure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Match(val => val.ToString(), (Func<Error, string>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("onFailure");
    }

    [Fact]
    public void Map_NullMapper_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.Map((Func<int, string>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mapper");
    }

    [Fact]
    public void OnSuccess_NullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        Action act = () => result.OnSuccess(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public void OnFailure_NullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");

        // Act
        Action act = () => result.OnFailure(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("action");
    }

    #endregion

    #region Default Result Tests

    [Fact]
    public void DefaultResult_AccessingValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var defaultResult = default(Result<int>);

        // Act
        Action act = () => _ = defaultResult.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void DefaultResult_AccessingError_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var defaultResult = default(Result<int>);

        // Act
        Action act = () => _ = defaultResult.Error;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    [Fact]
    public void DefaultResult_Map_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var defaultResult = default(Result<int>);

        // Act - Map tries to access Error on invalid state, which throws
        Action act = () => defaultResult.Map(x => x.ToString());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    [Fact]
    public void DefaultResult_OnSuccess_ShouldNotExecute()
    {
        // Arrange
        var defaultResult = default(Result<int>);
        var executed = false;

        // Act
        defaultResult.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void DefaultResult_OnFailure_ShouldThrowWhenAccessingError()
    {
        // Arrange
        var defaultResult = default(Result<int>);

        // Act
        Action act = () => defaultResult.OnFailure(err => { var _ = err.Code; });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid state*");
    }

    #endregion

    #region Exception Propagation Tests

    [Fact]
    public void Map_MapperThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Action act = () => result.Map<int, string>(_ => throw expectedException);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void OnSuccess_ActionThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Action act = () => result.OnSuccess(_ => throw expectedException);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void OnFailure_ActionThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Action act = () => result.OnFailure(_ => throw expectedException);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void Match_OnSuccessThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Action act = () => result.Match<int, string>(
            _ => throw expectedException,
            err => err.Code);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void Match_OnFailureThrows_ShouldPropagateException()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Action act = () => result.Match<int, string>(
            val => val.ToString(),
            _ => throw expectedException);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void ChainedOperations_ExceptionInterruptsChain()
    {
        // Arrange
        var result = Result<int>.Success(1);
        var counter = 0;

        // Act
        Action act = () => result
            .Map(x => { counter++; return x + 1; })
            .Map(x => { counter++; return x + 1; })
            .Map<int, int>(_ => throw new InvalidOperationException("Interrupt"))
            .Map(x => { counter++; return x + 1; })
            .Map(x => { counter++; return x + 1; });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Interrupt");
        counter.Should().Be(2); // Only first two maps executed
    }

    #endregion

    #region Mixed Chain Tests

    [Fact]
    public void MixedChain_MapOnSuccessOnFailure_Success()
    {
        // Arrange
        var result = Result<int>.Success(10);
        var log = new List<string>();

        // Act
        var final = result
            .OnSuccess(x => log.Add($"Initial: {x}"))
            .Map(x => x * 2)
            .OnSuccess(x => log.Add($"After *2: {x}"))
            .OnFailure(e => log.Add($"Error: {e.Code}"))
            .Map(x => x + 5)
            .OnSuccess(x => log.Add($"Final: {x}"));

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be(25);
        log.Should().BeEquivalentTo(new[] { "Initial: 10", "After *2: 20", "Final: 25" });
    }

    [Fact]
    public void MixedChain_MapOnSuccessOnFailure_Failure()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Failed");
        var log = new List<string>();

        // Act
        var final = result
            .OnSuccess(x => log.Add($"Initial: {x}"))
            .Map(x => x * 2)
            .OnSuccess(x => log.Add($"After *2: {x}"))
            .OnFailure(e => log.Add($"Error: {e.Code}"))
            .Map(x => x + 5)
            .OnSuccess(x => log.Add($"Final: {x}"));

        // Assert
        final.IsFailure.Should().BeTrue();
        final.Error.Code.Should().Be("ERR");
        log.Should().BeEquivalentTo(new[] { "Error: ERR" });
    }

    [Fact]
    public void MixedChain_MultipleOnFailure_ShouldExecuteAll()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Failed");
        var log = new List<string>();

        // Act
        result
            .OnFailure(e => log.Add($"Handler1: {e.Code}"))
            .OnFailure(e => log.Add($"Handler2: {e.Message}"))
            .OnFailure(e => log.Add("Handler3"));

        // Assert
        log.Should().BeEquivalentTo(new[] { "Handler1: ERR", "Handler2: Failed", "Handler3" });
    }

    #endregion

    #region Type Transformation Tests

    [Fact]
    public void Map_StructToStruct_ShouldNotBox()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => new TestStruct { Value = x });

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Map_StructToClass_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => new TestClass { Value = x });

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Map_ClassToStruct_ShouldWork()
    {
        // Arrange
        var result = Result<TestClass>.Success(new TestClass { Value = 42 });

        // Act
        var mapped = result.Map(x => new TestStruct { Value = x.Value });

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Map_ToValueTuple_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => (x, x.ToString()));

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be((42, "42"));
    }

    [Fact]
    public void Match_ReturnsStruct_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(
            x => new TestStruct { Value = x },
            _ => new TestStruct { Value = -1 });

        // Assert
        output.Value.Should().Be(42);
    }

    #endregion

    #region Concurrent Usage Tests

    [Fact]
    public async Task ConcurrentFluentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var syncLock = new object();

        // Act - Run 100 parallel fluent chains
        for (int i = 0; i < 100; i++)
        {
            var localI = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var result = Result<int>.Success(localI)
                        .Map(x => x * 2)
                        .OnSuccess(_ => { })
                        .Map(x => x + 1)
                        .Match(
                            x => x,
                            _ => -1);

                    if (result != localI * 2 + 1)
                    {
                        throw new InvalidOperationException($"Mismatch: expected {localI * 2 + 1}, got {result}");
                    }
                }
                catch (Exception ex)
                {
                    lock (syncLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion

    #region Helper Types

    private struct TestStruct
    {
        public int Value { get; set; }
    }

    private class TestClass
    {
        public int Value { get; set; }
    }

    #endregion
}
