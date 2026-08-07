using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Verdict.Async;
using Xunit;

namespace Verdict.Async.Tests;

public class WithTimeoutTests
{
    private static readonly Error TimeoutError = new("TIMEOUT", "Operation timed out");

    private static async Task<Result<int>> SlowAsync(TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        return Result<int>.Success(1);
    }

    private static async Task<Result> SlowVoidAsync(TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        return Result.Success();
    }

    [Fact]
    public async Task WithTimeout_WhenOperationIsSlow_ReturnsTimeoutError()
    {
        var result = await SlowAsync(TimeSpan.FromSeconds(5))
            .WithTimeout(TimeSpan.FromMilliseconds(100), TimeoutError);

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error.Code);
    }

    [Fact]
    public async Task WithTimeout_WhenOperationIsFast_ReturnsTheValue()
    {
        var result = await SlowAsync(TimeSpan.FromMilliseconds(1))
            .WithTimeout(TimeSpan.FromSeconds(5), TimeoutError);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    /// <summary>
    /// The overload taking a CancellationToken must still honour the timeout.
    /// It previously created a linked source, called CancelAfter, and never
    /// passed the token to anything, so the timeout had no effect at all.
    /// </summary>
    [Fact]
    public async Task WithTimeout_WithCancellationToken_StillTimesOut()
    {
        using var cts = new CancellationTokenSource();
        var sw = Stopwatch.StartNew();

        var result = await SlowAsync(TimeSpan.FromSeconds(5))
            .WithTimeout(TimeSpan.FromMilliseconds(100), TimeoutError, cts.Token);

        sw.Stop();

        Assert.True(result.IsFailure, "the operation took 5s against a 100ms timeout");
        Assert.Equal("TIMEOUT", result.Error.Code);
        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"returned after {sw.ElapsedMilliseconds}ms, so the timeout was not applied");
    }

    [Fact]
    public async Task WithTimeout_WithCancellationToken_WhenFast_ReturnsTheValue()
    {
        using var cts = new CancellationTokenSource();

        var result = await SlowAsync(TimeSpan.FromMilliseconds(1))
            .WithTimeout(TimeSpan.FromSeconds(5), TimeoutError, cts.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    /// <summary>
    /// A caller cancelling is different from the operation timing out, and the
    /// two must not be reported the same way.
    /// </summary>
    [Fact]
    public async Task WithTimeout_WhenCallerCancels_DoesNotReportATimeout()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var task = SlowAsync(TimeSpan.FromSeconds(5))
            .WithTimeout(TimeSpan.FromSeconds(30), TimeoutError, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_WhenSlow_ReturnsTimeoutError()
    {
        var result = await SlowVoidAsync(TimeSpan.FromSeconds(5))
            .WithTimeout(TimeSpan.FromMilliseconds(100), TimeoutError);

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error.Code);
    }

    [Fact]
    public async Task WithTimeout_NullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> nullTask = null!;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => nullTask.WithTimeout(TimeSpan.FromSeconds(1), TimeoutError));
    }

    [Fact]
    public async Task WithTimeout_NegativeTimeout_ThrowsArgumentOutOfRange()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => SlowAsync(TimeSpan.FromMilliseconds(1))
                .WithTimeout(TimeSpan.FromSeconds(-1), TimeoutError));
    }

    [Fact]
    public async Task EnsureAsync_NullPredicate_ThrowsArgumentNullException()
    {
        Func<int, Task<bool>> predicate = null!;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(Result<int>.Success(1)).EnsureAsync(predicate, TimeoutError));
    }
}
