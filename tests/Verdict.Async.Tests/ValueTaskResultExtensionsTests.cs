using System;
using System.Threading;
using System.Threading.Tasks;
using Verdict.Async;
using Xunit;

namespace Verdict.Async.Tests;

/// <summary>
/// Behaviour of the ValueTask overloads, on both the completed fast path and the
/// genuinely pending one. The allocation gate covers the byte count; this covers
/// whether the two paths agree.
/// </summary>
public class ValueTaskResultExtensionsTests
{
    private static ValueTask<Result<int>> Completed(int value) =>
        new(Result<int>.Success(value));

    private static ValueTask<Result<int>> CompletedFailure(Error error) =>
        new(Result<int>.Failure(error));

    /// <summary>
    /// A ValueTask that has not finished yet, so the awaiting path is taken.
    /// </summary>
    private static async ValueTask<Result<int>> Pending(int value)
    {
        await Task.Yield();
        return Result<int>.Success(value);
    }

    private static async ValueTask<Result<int>> PendingFailure(Error error)
    {
        await Task.Yield();
        return Result<int>.Failure(error);
    }

    private static readonly Error Missing = new("NOT_FOUND", "missing");

    [Fact]
    public async Task MapOnACompletedSuccess()
    {
        var result = await Completed(21).Map(x => x * 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task MapOnAPendingSuccess()
    {
        var result = await Pending(21).Map(x => x * 2);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task MapCarriesTheErrorThroughOnBothPaths()
    {
        var fromCompleted = await CompletedFailure(Missing).Map(x => x * 2);
        var fromPending = await PendingFailure(Missing).Map(x => x * 2);

        Assert.True(fromCompleted.IsFailure);
        Assert.True(fromPending.IsFailure);
        Assert.Equal("NOT_FOUND", fromCompleted.Error.Code);
        Assert.Equal(fromCompleted.Error.Code, fromPending.Error.Code);
    }

    [Fact]
    public async Task MapDoesNotRunTheMapperOnAFailure()
    {
        var ran = false;

        await CompletedFailure(Missing).Map(x => { ran = true; return x; });

        Assert.False(ran);
    }

    [Fact]
    public async Task MapAsyncOnACompletedInner()
    {
        var result = await Completed(21).MapAsync(x => new ValueTask<int>(x * 2));

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task MapAsyncOnAPendingInner()
    {
        var result = await Completed(21).MapAsync(async x =>
        {
            await Task.Yield();
            return x * 2;
        });

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task BindChainsAndShortCircuits()
    {
        var ok = await Completed(21).Bind(x => Result<int>.Success(x * 2));
        var bad = await CompletedFailure(Missing).Bind(x => Result<int>.Success(x * 2));

        Assert.Equal(42, ok.Value);
        Assert.True(bad.IsFailure);
    }

    [Fact]
    public async Task BindAsyncChainsAndShortCircuits()
    {
        var ok = await Completed(21).BindAsync(x => new ValueTask<Result<int>>(Result<int>.Success(x * 2)));
        var bad = await CompletedFailure(Missing).BindAsync(x => new ValueTask<Result<int>>(Result<int>.Success(x)));

        Assert.Equal(42, ok.Value);
        Assert.True(bad.IsFailure);
    }

    [Fact]
    public async Task TapRunsOnSuccessOnly()
    {
        var seen = 0;

        await Completed(21).Tap(x => seen = x);
        await CompletedFailure(Missing).Tap(x => seen = 99);

        Assert.Equal(21, seen);
    }

    [Fact]
    public async Task TapAsyncRunsOnSuccessOnlyAndPassesTheResultThrough()
    {
        var seen = 0;

        var result = await Completed(21).TapAsync(x =>
        {
            seen = x;
            return default;
        });

        Assert.Equal(21, seen);
        Assert.Equal(21, result.Value);
    }

    [Fact]
    public async Task TapErrorAsyncRunsOnFailureOnly()
    {
        string? seen = null;

        await Completed(21).TapErrorAsync(e => { seen = e.Code; return default; });
        Assert.Null(seen);

        await CompletedFailure(Missing).TapErrorAsync(e => { seen = e.Code; return default; });
        Assert.Equal("NOT_FOUND", seen);
    }

    [Fact]
    public async Task EnsureFailsWhenThePredicateDoesNotHold()
    {
        var kept = await Completed(21).Ensure(x => x > 0, Missing);
        var rejected = await Completed(-1).Ensure(x => x > 0, Missing);

        Assert.True(kept.IsSuccess);
        Assert.True(rejected.IsFailure);
        Assert.Equal("NOT_FOUND", rejected.Error.Code);
    }

    [Fact]
    public async Task MatchUnwrapsBothBranches()
    {
        var fromSuccess = await Completed(21).Match(x => x * 2, _ => -1);
        var fromFailure = await CompletedFailure(Missing).Match(x => x * 2, _ => -1);

        Assert.Equal(42, fromSuccess);
        Assert.Equal(-1, fromFailure);
    }

    [Fact]
    public async Task MatchUnwrapsBothBranchesWhenPending()
    {
        var fromSuccess = await Pending(21).Match(x => x * 2, _ => -1);
        var fromFailure = await PendingFailure(Missing).Match(x => x * 2, _ => -1);

        Assert.Equal(42, fromSuccess);
        Assert.Equal(-1, fromFailure);
    }

    [Fact]
    public async Task MatchAsyncUnwrapsBothBranches()
    {
        var fromSuccess = await Completed(21).MatchAsync(
            x => new ValueTask<int>(x * 2),
            _ => new ValueTask<int>(-1));
        var fromFailure = await CompletedFailure(Missing).MatchAsync(
            x => new ValueTask<int>(x * 2),
            _ => new ValueTask<int>(-1));

        Assert.Equal(42, fromSuccess);
        Assert.Equal(-1, fromFailure);
    }

    [Fact]
    public async Task AFullPipelineReads()
    {
        var response = await Result<int>.Success(20)
            .AsValueTask()
            .Ensure(x => x > 0, Missing)
            .Map(x => x + 1)
            .Bind(x => Result<int>.Success(x * 2))
            .Match(x => $"ok:{x}", e => $"bad:{e.Code}");

        Assert.Equal("ok:42", response);
    }

    [Fact]
    public async Task AFullPipelineShortCircuitsOnTheFirstFailure()
    {
        var mapped = false;

        var response = await Result<int>.Success(-5)
            .AsValueTask()
            .Ensure(x => x > 0, Missing)
            .Map(x => { mapped = true; return x + 1; })
            .Match(x => $"ok:{x}", e => $"bad:{e.Code}");

        Assert.Equal("bad:NOT_FOUND", response);
        Assert.False(mapped);
    }

    [Fact]
    public async Task ATaskCanFeedAValueTaskPipeline()
    {
        var response = await Task.FromResult(Result<int>.Success(21))
            .AsValueTask()
            .Map(x => x * 2)
            .Match(x => x, _ => -1);

        Assert.Equal(42, response);
    }

    [Fact]
    public async Task CancellationIsObservedBeforeTheMapperRuns()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Completed(21).MapAsync((x, _) => new ValueTask<int>(x), source.Token));
    }

    [Theory]
    [InlineData("map")]
    [InlineData("bind")]
    [InlineData("tap")]
    [InlineData("match")]
    public async Task ANullDelegateIsRejected(string operation)
    {
        var task = Completed(21);

        await Assert.ThrowsAsync<ArgumentNullException>(() => operation switch
        {
            "map" => task.Map<int, int>(null!).AsTask(),
            "bind" => task.Bind<int, int>(null!).AsTask(),
            "tap" => task.Tap(null!).AsTask(),
            _ => task.Match<int, int>(null!, _ => 0).AsTask(),
        });
    }

    [Fact]
    public async Task MatchAsyncExistsOnTheTaskApiToo()
    {
        var response = await Task.FromResult(Result<int>.Success(21))
            .MatchAsync(x => x * 2, _ => -1);

        Assert.Equal(42, response);
    }
}
