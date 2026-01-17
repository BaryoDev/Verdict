using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Verdict.Async;
using Xunit;

namespace Verdict.Async.Tests;

/// <summary>
/// Comprehensive edge case and security tests for Async Result extensions.
/// Tests for cancellation token handling, timeout boundaries, concurrent execution, and exception propagation.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region CancellationToken Edge Cases

    [Fact]
    public async Task MapAsync_WithAlreadyCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await resultTask.MapAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x.ToString();
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task BindAsync_WithAlreadyCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await resultTask.BindAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return Result<string>.Success(x.ToString());
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TapAsync_WithAlreadyCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await resultTask.TapAsync(
            async (_, ct) =>
            {
                await Task.Delay(10, ct);
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EnsureAsync_WithAlreadyCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await resultTask.EnsureAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x > 0;
            },
            new Error("ERR", "Msg"),
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task MapAsync_CancellationMidExecution_ShouldThrow()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();

        // Act
        Func<Task> act = async () => await resultTask.MapAsync(
            async (x, ct) =>
            {
                await Task.Delay(500, ct);
                return x.ToString();
            },
            cts.Token);

        // Cancel after a short delay
        _ = Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task NonGenericBindAsync_WithAlreadyCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await resultTask.BindAsync(
            async ct =>
            {
                await Task.Delay(10, ct);
                return Result.Success();
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Timeout Boundary Tests

    [Fact]
    public async Task WithTimeout_ZeroTimeout_ShouldReturnTimeoutError()
    {
        // Arrange
        var resultTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            return Result<int>.Success(42);
        });

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.Zero,
            new Error("TIMEOUT", "Operation timed out"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TIMEOUT");
    }

    [Fact]
    public async Task WithTimeout_VeryLongTimeout_ShouldComplete()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.FromHours(1),
            new Error("TIMEOUT", "Operation timed out"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task WithTimeout_FailureResult_ShouldPropagateError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Failure("ORIGINAL_ERROR", "Original message"));

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.FromSeconds(5),
            new Error("TIMEOUT", "Should not see this"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ORIGINAL_ERROR");
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_ZeroTimeout_ShouldReturnTimeoutError()
    {
        // Arrange
        var resultTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            return Result.Success();
        });

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.Zero,
            new Error("TIMEOUT", "Operation timed out"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TIMEOUT");
    }

    #endregion

    #region Concurrent Execution Tests

    [Fact]
    public async Task ConcurrentAsyncChains_ShouldNotInterfere()
    {
        // Arrange
        var tasks = new List<Task<Result<int>>>();
        var exceptions = new List<Exception>();

        // Act - Run 100 parallel async chains
        for (int i = 0; i < 100; i++)
        {
            var localI = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await Task.FromResult(Result<int>.Success(localI))
                        .MapAsync(async x =>
                        {
                            await Task.Yield();
                            return x * 2;
                        })
                        .BindAsync(async x =>
                        {
                            await Task.Yield();
                            return Result<int>.Success(x + 1);
                        });
                    return result;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    return Result<int>.Failure("ERROR", ex.Message);
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
        for (int i = 0; i < 100; i++)
        {
            results[i].IsSuccess.Should().BeTrue();
            results[i].Value.Should().Be(i * 2 + 1);
        }
    }

    [Fact]
    public async Task ConcurrentCancellations_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task>();
        var cancellations = 0;
        var completions = 0;
        var syncLock = new object();

        // Act - Run 100 parallel operations with random cancellations
        for (int i = 0; i < 100; i++)
        {
            var cts = new CancellationTokenSource();
            if (i % 2 == 0) cts.Cancel(); // Cancel every other one

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await Task.FromResult(Result<int>.Success(42))
                        .MapAsync(
                            async (x, ct) =>
                            {
                                await Task.Delay(10, ct);
                                return x.ToString();
                            },
                            cts.Token);

                    lock (syncLock) completions++;
                }
                catch (OperationCanceledException)
                {
                    lock (syncLock) cancellations++;
                }
                finally
                {
                    cts.Dispose();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        cancellations.Should().Be(50);
        completions.Should().Be(50);
    }

    #endregion

    #region Null Handler Tests

    [Fact]
    public async Task MapAsync_NullMapper_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.MapAsync((Func<int, Task<string>>)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("mapper");
    }

    [Fact]
    public async Task MapAsync_WithCancellation_NullMapper_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();

        // Act
        Func<Task> act = async () => await resultTask.MapAsync(
            (Func<int, CancellationToken, Task<string>>)null!,
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("mapper");
    }

    [Fact]
    public async Task BindAsync_NullBinder_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.BindAsync((Func<int, Task<Result<string>>>)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("binder");
    }

    [Fact]
    public async Task TapAsync_NullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.TapAsync((Func<int, Task>)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public async Task TapErrorAsync_NullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Failure("ERR", "Msg"));

        // Act
        Func<Task> act = async () => await resultTask.TapErrorAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("action");
    }

    [Fact]
    public async Task EnsureAsync_NullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.EnsureAsync(
            (Func<int, Task<bool>>)null!,
            new Error("ERR", "Msg"));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("predicate");
    }

    [Fact]
    public async Task EnsureAsync_WithErrorFactory_NullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.EnsureAsync(
            (Func<int, Task<bool>>)null!,
            x => new Error("ERR", $"Value: {x}"));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("predicate");
    }

    [Fact]
    public async Task EnsureAsync_WithErrorFactory_NullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.EnsureAsync(
            async x =>
            {
                await Task.Yield();
                return false;
            },
            (Func<int, Error>)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("errorFactory");
    }

    #endregion

    #region Exception Propagation Tests

    [Fact]
    public async Task MapAsync_MapperThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = async () => await resultTask.MapAsync<int, string>(async _ =>
        {
            await Task.Yield();
            throw expectedException;
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task BindAsync_BinderThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = async () => await resultTask.BindAsync<int, string>(async _ =>
        {
            await Task.Yield();
            throw expectedException;
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task TapAsync_ActionThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = async () => await resultTask.TapAsync(async _ =>
        {
            await Task.Yield();
            throw expectedException;
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region Task State Tests

    [Fact]
    public async Task MapAsync_WithFaultedTask_ShouldPropagateException()
    {
        // Arrange
        var faultedTask = Task.FromException<Result<int>>(new InvalidOperationException("Faulted"));

        // Act
        Func<Task> act = async () => await faultedTask.MapAsync(async x =>
        {
            await Task.Yield();
            return x.ToString();
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Faulted");
    }

    [Fact]
    public async Task MapAsync_WithCancelledTask_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelledTask = Task.FromCanceled<Result<int>>(cts.Token);

        // Act
        Func<Task> act = async () => await cancelledTask.MapAsync(async x =>
        {
            await Task.Yield();
            return x.ToString();
        });

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
        cts.Dispose();
    }

    [Fact]
    public async Task MapAsync_WithCompletedTask_ShouldWork()
    {
        // Arrange
        var completedTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var result = await completedTask.MapAsync(async x =>
        {
            await Task.Yield();
            return x.ToString();
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("42");
    }

    #endregion
}
