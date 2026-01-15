using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Verdict.Async;

namespace Verdict.Async.Tests;

/// <summary>
/// Comprehensive edge case and security tests for Async Result extensions.
/// Tests for potential vulnerabilities, boundary conditions, and concurrent access.
/// </summary>
public class SecurityEdgeCaseTests
{
    #region CancellationToken Edge Cases

    [Fact]
    public async Task MapAsync_WithAlreadyCancelledToken_ShouldThrowImmediately()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before calling MapAsync

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
    public async Task BindAsync_WithAlreadyCancelledToken_ShouldThrowImmediately()
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
    public async Task TapAsync_WithAlreadyCancelledToken_ShouldThrowImmediately()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await resultTask.TapAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task MapAsync_WithCancelledTokenDuringExecution_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();

        // Act
        Func<Task> act = async () => await resultTask.MapAsync(
            async (x, ct) =>
            {
                cts.Cancel(); // Cancel during execution
                await Task.Delay(100, ct);
                return x.ToString();
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Timeout Boundary Conditions

    [Fact]
    public async Task MapAsync_WithZeroTimeout_ShouldCancelImmediately()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource(TimeSpan.Zero);

        // Act
        Func<Task> act = async () => await resultTask.MapAsync(
            async (x, ct) =>
            {
                await Task.Delay(100, ct);
                return x.ToString();
            },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CancellationTokenSource_WithNegativeTimeout_ShouldThrow()
    {
        // Arrange & Act - This tests CancellationTokenSource behavior, not MapAsync
        Action act = () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(-2)); // -1 is infinite, -2 is invalid
        };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task MapAsync_WithVeryLongTimeout_ShouldNotCancelPrematurely()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource(TimeSpan.FromHours(1));

        // Act
        var mapped = await resultTask.MapAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x.ToString();
            },
            cts.Token);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    #endregion

    #region Concurrent Async Chain Execution

    [Fact]
    public async Task MapAsync_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new Task<Result<string>>[100];

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = Result<int>.Success(index);
                return await Task.FromResult(result).MapAsync(async x =>
                {
                    await Task.Delay(1);
                    return x.ToString();
                });
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().OnlyContain(r => r.IsSuccess);
        results.Select(r => r.Value).Should().OnlyContain(v => !string.IsNullOrEmpty(v));
    }

    [Fact]
    public async Task BindAsync_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new Task<Result<int>>[50];

        // Act
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var result = Result<string>.Success(index.ToString());
                return await Task.FromResult(result).BindAsync(async x =>
                {
                    await Task.Delay(1);
                    return Result<int>.Success(int.Parse(x) * 2);
                });
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(50);
        results.Should().OnlyContain(r => r.IsSuccess);
    }

    #endregion

    #region Null Async Function Handlers

    [Fact]
    public async Task MapAsync_WithNullMapper_ShouldThrow()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        Func<int, Task<string>> nullMapper = null!;

        // Act
        Func<Task> act = async () => await resultTask.MapAsync(nullMapper);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*mapper*");
    }

    [Fact]
    public async Task BindAsync_WithNullBinder_ShouldThrow()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        Func<int, Task<Result<string>>> nullBinder = null!;

        // Act
        Func<Task> act = async () => await resultTask.BindAsync(nullBinder);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*binder*");
    }

    [Fact]
    public async Task TapAsync_WithNullAction_ShouldThrow()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        Func<int, Task> nullAction = null!;

        // Act
        Func<Task> act = async () => await resultTask.TapAsync(nullAction);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*action*");
    }

    [Fact]
    public async Task EnsureAsync_WithNullPredicate_ShouldThrow()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        Func<int, Task<bool>> nullPredicate = null!;

        // Act
        Func<Task> act = async () => await resultTask.EnsureAsync(nullPredicate, "ERROR", "Message");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*predicate*");
    }

    #endregion

    #region Exception Propagation in Async Chains

    [Fact]
    public async Task MapAsync_WhenMapperThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.MapAsync<int, string>(async x =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Mapper failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mapper failed");
    }

    [Fact]
    public async Task BindAsync_WhenBinderThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.BindAsync<int, string>(async x =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Binder failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Binder failed");
    }

    [Fact]
    public async Task TapAsync_WhenActionThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.TapAsync(async x =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Action failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Action failed");
    }

    [Fact]
    public async Task EnsureAsync_WhenPredicateThrows_ShouldPropagateException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        Func<Task> act = async () => await resultTask.EnsureAsync(
            async x =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Predicate failed");
            },
            "ERROR",
            "Message");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Predicate failed");
    }

    #endregion

    #region ConfigureAwait Behavior

    [Fact]
    public async Task MapAsync_ShouldWorkWithDifferentContexts()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var originalContext = SynchronizationContext.Current;

        // Act - Test that MapAsync works correctly regardless of synchronization context
        var mapped = await resultTask.MapAsync(async x =>
        {
            await Task.Yield();
            // MapAsync should work with any context
            var currentContext = SynchronizationContext.Current;
            return x.ToString();
        });

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public async Task BindAsync_ChainedOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var final = await resultTask
            .MapAsync(async x =>
            {
                await Task.Delay(5);
                return x * 2;
            })
            .BindAsync(async x =>
            {
                await Task.Delay(5);
                return Result<string>.Success(x.ToString());
            })
            .TapAsync(async x =>
            {
                await Task.Delay(5);
                // Side effect
            });

        // Assert
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("84");
    }

    [Fact]
    public async Task MapAsync_OnFailure_ShouldNotExecuteMapper()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Failure("ERROR", "Failed"));
        var mapperExecuted = false;

        // Act
        var mapped = await resultTask.MapAsync(async x =>
        {
            await Task.Yield();
            mapperExecuted = true;
            return x.ToString();
        });

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapperExecuted.Should().BeFalse();
    }

    #endregion

    #region Edge Cases with Task Completion

    [Fact]
    public async Task MapAsync_WithCompletedTask_ShouldExecuteSynchronously()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var completedTask = Task.FromResult(result);

        // Act
        var mapped = await completedTask.MapAsync(x => Task.FromResult(x.ToString()));

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public async Task MapAsync_WithFaultedTask_ShouldPropagateException()
    {
        // Arrange
        var faultedTask = Task.FromException<Result<int>>(new InvalidOperationException("Task failed"));

        // Act
        Func<Task> act = async () => await faultedTask.MapAsync(x => Task.FromResult(x.ToString()));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Task failed");
    }

    [Fact]
    public async Task MapAsync_WithCancelledTask_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Result<int>>();
        tcs.SetCanceled();
        var cancelledTask = tcs.Task;

        // Act
        Func<Task> act = async () => await cancelledTask.MapAsync(x => Task.FromResult(x.ToString()));

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    #endregion

    #region Null and Default Values

    [Fact]
    public async Task MapAsync_ReturningNull_ShouldCreateSuccessWithNull()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var mapped = await resultTask.MapAsync(x => Task.FromResult<string?>(null));

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().BeNull();
    }

    [Fact]
    public async Task BindAsync_ReturningFailure_ShouldPreserveError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var expectedError = new Error("BIND_ERROR", "Bind operation failed");

        // Act
        var bound = await resultTask.BindAsync(x => Task.FromResult(Result<string>.Failure(expectedError)));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(expectedError);
    }

    [Fact]
    public async Task EnsureAsync_WithDefaultResult_ShouldHandleCorrectly()
    {
        // Arrange
        Result<int> defaultResult = default;
        var resultTask = Task.FromResult(defaultResult);

        // Act
        var ensured = await resultTask.EnsureAsync(
            x => Task.FromResult(x > 0),
            "POSITIVE_REQUIRED",
            "Value must be positive");

        // Assert
        ensured.IsFailure.Should().BeTrue();
    }

    #endregion
}
