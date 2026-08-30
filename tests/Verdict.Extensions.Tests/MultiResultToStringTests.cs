using System.Collections.Generic;
using System.Linq;
using Verdict.Extensions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// A ToString that throws breaks the debugger and the log line in exactly the
/// situation you are trying to understand.
/// </summary>
public class MultiResultToStringTests
{
    private static MultiResult<int> PooledFailure()
    {
        var errors = Enumerable.Range(0, ErrorCollection.PoolingThreshold + 2)
            .Select(i => new Error($"E{i}", $"message {i}"))
            .ToList();

        return MultiResult<int>.Failure(ErrorCollection.Create((IEnumerable<Error>)errors));
    }

    [Fact]
    public void ToStringAfterDisposeErrorsDoesNotThrow()
    {
        var result = PooledFailure();
        result.DisposeErrors();

        Assert.Equal("Failure(errors released)", result.ToString());
    }

    [Fact]
    public void NonGenericToStringAfterDisposeErrorsDoesNotThrow()
    {
        var errors = Enumerable.Range(0, ErrorCollection.PoolingThreshold + 2)
            .Select(i => new Error($"E{i}", $"message {i}"))
            .ToList();
        var result = MultiResult.Failure(ErrorCollection.Create((IEnumerable<Error>)errors));
        result.DisposeErrors();

        Assert.Equal("Failure(errors released)", result.ToString());
    }

    [Fact]
    public void ToStringStillReportsTheCountWhileTheErrorsAreLive()
    {
        var result = PooledFailure();

        Assert.Equal($"Failure({ErrorCollection.PoolingThreshold + 2} error(s))", result.ToString());
    }
}
