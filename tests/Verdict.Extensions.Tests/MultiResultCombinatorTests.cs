using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verdict.Extensions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Once validation accumulates errors you are holding a MultiResult, and until
/// now that was the end of the chaining API: no Map, no Bind, no Match. These
/// cover the combinators that keep an accumulated result composable.
/// </summary>
public class MultiResultCombinatorTests
{
    private static MultiResult<int> Ok(int v) => MultiResult<int>.Success(v);
    private static MultiResult<int> Bad(params string[] codes) =>
        MultiResult<int>.Failure(codes.Select(c => new Error(c, c + " failed")).ToArray());

    // ---------- Map ----------

    [Fact]
    public void Map_OnSuccess_TransformsTheValue()
    {
        MultiResult<string> mapped = Ok(21).Map(v => (v * 2).ToString());

        Assert.True(mapped.IsSuccess);
        Assert.Equal("42", mapped.Value);
    }

    [Fact]
    public void Map_OnFailure_KeepsEveryError()
    {
        MultiResult<string> mapped = Bad("A", "B", "C").Map(v => v.ToString());

        Assert.True(mapped.IsFailure);
        Assert.Equal(3, mapped.ErrorCount);
        Assert.Equal("A", mapped.Errors[0].Code);
        Assert.Equal("C", mapped.Errors[2].Code);
    }

    [Fact]
    public void Map_OnFailure_DoesNotInvokeTheMapper()
    {
        var called = false;
        Bad("A").Map<int, int>(v => { called = true; return v; });

        Assert.False(called);
    }

    [Fact]
    public void Map_NullMapper_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Ok(1).Map<int, int>(null!));
    }

    // ---------- Bind ----------

    [Fact]
    public void Bind_OnSuccess_ChainsTheNextStep()
    {
        var result = Ok(10).Bind(v => MultiResult<int>.Success(v + 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void Bind_WhenTheNextStepFails_SurfacesItsErrors()
    {
        var result = Ok(10).Bind(_ => Bad("X", "Y"));

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.ErrorCount);
    }

    [Fact]
    public void Bind_OnFailure_ShortCircuitsAndKeepsErrors()
    {
        var called = false;
        var result = Bad("A", "B").Bind(v => { called = true; return Ok(v); });

        Assert.False(called);
        Assert.Equal(2, result.ErrorCount);
    }

    // ---------- Match ----------

    [Fact]
    public void Match_OnSuccess_UsesTheSuccessBranch()
    {
        var text = Ok(7).Match(v => $"got {v}", errors => $"{errors.Count} problems");

        Assert.Equal("got 7", text);
    }

    [Fact]
    public void Match_OnFailure_ReceivesEveryError()
    {
        var text = Bad("A", "B", "C").Match(v => "ok", errors => $"{errors.Count} problems");

        Assert.Equal("3 problems", text);
    }

    [Fact]
    public void Match_NullBranch_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Ok(1).Match<int, string>(null!, _ => ""));
        Assert.Throws<ArgumentNullException>(() => Ok(1).Match<int, string>(_ => "", null!));
    }

    // ---------- Side effects ----------

    [Fact]
    public void OnSuccess_RunsOnlyWhenSuccessful_AndReturnsTheOriginal()
    {
        var seen = 0;
        var returned = Ok(5).OnSuccess(v => seen = v);

        Assert.Equal(5, seen);
        Assert.True(returned.IsSuccess);

        seen = 0;
        Bad("A").OnSuccess(v => seen = v);
        Assert.Equal(0, seen);
    }

    [Fact]
    public void OnFailure_ReceivesAllErrors_AndReturnsTheOriginal()
    {
        var count = 0;
        var returned = Bad("A", "B").OnFailure(errors => count = errors.Count);

        Assert.Equal(2, count);
        Assert.True(returned.IsFailure);
    }

    // ---------- Async ----------

    [Fact]
    public async Task MapAsync_TransformsOnSuccess()
    {
        var mapped = await Task.FromResult(Ok(21)).MapAsync(async v =>
        {
            await Task.Yield();
            return v * 2;
        });

        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task BindAsync_ShortCircuitsOnFailure()
    {
        var called = false;
        var result = await Task.FromResult(Bad("A")).BindAsync(async v =>
        {
            called = true;
            await Task.Yield();
            return Ok(v);
        });

        Assert.False(called);
        Assert.True(result.IsFailure);
    }

    // ---------- Interop with the rest of the library ----------

    [Fact]
    public void Combinators_ComposeWithEnsure()
    {
        var result = Result<int>.Success(4)
            .EnsureAll(
                (v => v > 0, new Error("POSITIVE", "must be positive")),
                (v => v % 2 == 0, new Error("EVEN", "must be even")))
            .Map(v => v * 10)
            .OnSuccess(_ => { });

        Assert.True(result.IsSuccess);
        Assert.Equal(40, result.Value);
    }

    [Fact]
    public void Combinators_PreserveAccumulatedErrorsThroughAChain()
    {
        var result = Result<int>.Success(-3)
            .EnsureAll(
                (v => v > 0, new Error("POSITIVE", "must be positive")),
                (v => v % 2 == 0, new Error("EVEN", "must be even")))
            .Map(v => v * 10);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.ErrorCount);
    }

    // ---------- Non-generic ----------

    [Fact]
    public void NonGeneric_OnFailure_AndMatch_Work()
    {
        var failed = MultiResult.Failure(new Error("A", "a"), new Error("B", "b"));

        var count = failed.Match(() => 0, errors => errors.Count);

        Assert.Equal(2, count);
    }
}
