using System;
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

/// <summary>
/// ToString has to survive a copy being disposed while it is running.
/// </summary>
public class MultiResultToStringRaceTests
{
    [Fact]
    public void ToStringNeverThrowsWhileAnotherCopyIsBeingDisposed()
    {
        // The two reads used to be IsDisposed and then Count, and Count throws
        // once the buffer is released. A copy disposing between them turned the
        // method that exists to survive disposal into the one that fails on it.
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var errors = Enumerable.Range(0, ErrorCollection.PoolingThreshold + 2)
                .Select(i => new Error($"E{i}", $"message {i}"))
                .ToList();
            var result = MultiResult<int>.Failure(ErrorCollection.Create((IEnumerable<Error>)errors));
            var copy = result;

            using var ready = new System.Threading.Barrier(2);
            string? text = null;
            Exception? thrown = null;

            var reader = System.Threading.Tasks.Task.Run(() =>
            {
                ready.SignalAndWait();
                try { text = result.ToString(); }
                catch (Exception ex) { thrown = ex; }
            });

            var disposer = System.Threading.Tasks.Task.Run(() =>
            {
                ready.SignalAndWait();
                copy.DisposeErrors();
            });

            System.Threading.Tasks.Task.WaitAll(reader, disposer);

            Assert.Null(thrown);
            Assert.NotNull(text);
        }
    }
}
