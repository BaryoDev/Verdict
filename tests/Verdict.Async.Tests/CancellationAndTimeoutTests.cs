using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Verdict.Async;

namespace Verdict.Async.Tests;

/// <summary>
/// Tests for CancellationToken support and timeout features.
/// </summary>
public class CancellationAndTimeoutTests
{
    #region MapAsync with CancellationToken

    [Fact]
    public async Task MapAsync_WithCancellationToken_Success_ShouldTransformValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();

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

    [Fact]
    public async Task MapAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

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

    #endregion

    #region BindAsync with CancellationToken

    [Fact]
    public async Task BindAsync_WithCancellationToken_Success_ShouldChainResults()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();

        // Act
        var bound = await resultTask.BindAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return Result<string>.Success(x.ToString());
            },
            cts.Token);

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    #endregion

    #region TapAsync with CancellationToken

    [Fact]
    public async Task TapAsync_WithCancellationToken_Success_ShouldExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();
        var executed = false;

        // Act
        await resultTask.TapAsync(
            async (_, ct) =>
            {
                await Task.Delay(10, ct);
                executed = true;
            },
            cts.Token);

        // Assert
        executed.Should().BeTrue();
    }

    #endregion

    #region EnsureAsync with CancellationToken

    [Fact]
    public async Task EnsureAsync_WithCancellationToken_PredicateTrue_ShouldReturnSuccess()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        using var cts = new CancellationTokenSource();

        // Act
        var result = await resultTask.EnsureAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x > 0;
            },
            new Error("INVALID", "Value must be positive"),
            cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_WithCancellationToken_PredicateFalse_ShouldReturnFailure()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(-1));
        using var cts = new CancellationTokenSource();

        // Act
        var result = await resultTask.EnsureAsync(
            async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x > 0;
            },
            new Error("INVALID", "Value must be positive"),
            cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID");
    }

    #endregion

    #region WithTimeout Tests

    [Fact]
    public async Task WithTimeout_CompletesBeforeTimeout_ShouldReturnResult()
    {
        // Arrange
        var resultTask = Task.Run(async () =>
        {
            await Task.Delay(10);
            return Result<int>.Success(42);
        });

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.FromSeconds(5),
            new Error("TIMEOUT", "Operation timed out"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task WithTimeout_ExceedsTimeout_ShouldReturnTimeoutError()
    {
        // Arrange
        var resultTask = Task.Run(async () =>
        {
            await Task.Delay(5000);
            return Result<int>.Success(42);
        });

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.FromMilliseconds(50),
            new Error("TIMEOUT", "Operation timed out"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TIMEOUT");
    }

    [Fact]
    public async Task WithTimeout_WithCodeAndMessage_ShouldUseProvidedError()
    {
        // Arrange
        var resultTask = Task.Run(async () =>
        {
            await Task.Delay(5000);
            return Result<int>.Success(42);
        });

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.FromMilliseconds(50),
            "CUSTOM_TIMEOUT",
            "Custom timeout message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CUSTOM_TIMEOUT");
        result.Error.Message.Should().Be("Custom timeout message");
    }

    [Fact]
    public async Task WithTimeout_NonGenericResult_CompletesBeforeTimeout_ShouldReturnSuccess()
    {
        // Arrange
        var resultTask = Task.Run(async () =>
        {
            await Task.Delay(10);
            return Result.Success();
        });

        // Act
        var result = await resultTask.WithTimeout(
            TimeSpan.FromSeconds(5),
            new Error("TIMEOUT", "Operation timed out"));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region EnsureAsync with Error Factory

    [Fact]
    public async Task EnsureAsync_WithErrorFactory_PredicateFalse_ShouldUseDynamicError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(5));

        // Act
        var result = await resultTask.EnsureAsync(
            async x =>
            {
                await Task.Yield();
                return x > 10;
            },
            x => new Error("BELOW_MIN", $"Value {x} is below minimum of 10"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BELOW_MIN");
        result.Error.Message.Should().Be("Value 5 is below minimum of 10");
    }

    #endregion
}
