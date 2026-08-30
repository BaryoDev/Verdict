using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Verdict.Logging;
using Xunit;

namespace Verdict.Logging.Tests;

/// <summary>
/// The extension methods dropped the exception while ResultLogger kept it, so
/// the version a caller reaches from result.LogError was the lossy one.
/// </summary>
public class ExceptionPropagationTests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<Exception?> Exceptions { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Exceptions.Add(exception);
    }

    private static readonly InvalidOperationException Cause = new("the database rejected the write");

    [Fact]
    public void LogErrorKeepsTheException()
    {
        var logger = new CapturingLogger();
        var result = Result<int>.Failure(new Error("DB_ERROR", "write failed", Cause));

        result.LogError(logger, "saving the order");

        Assert.Same(Cause, Assert.Single(logger.Exceptions));
    }

    [Fact]
    public void LogKeepsTheExceptionOnTheFailureBranch()
    {
        var logger = new CapturingLogger();
        var result = Result<int>.Failure(new Error("DB_ERROR", "write failed", Cause));

        result.Log(logger, "saving the order");

        Assert.Same(Cause, Assert.Single(logger.Exceptions));
    }

    [Fact]
    public void NonGenericLogErrorKeepsTheException()
    {
        var logger = new CapturingLogger();
        var result = Result.Failure(new Error("DB_ERROR", "write failed", Cause));

        result.LogError(logger, "saving the order");

        Assert.Same(Cause, Assert.Single(logger.Exceptions));
    }

    [Fact]
    public void AnErrorWithNoExceptionLogsWithoutOne()
    {
        var logger = new CapturingLogger();
        var result = Result<int>.Failure(new Error("NOT_FOUND", "missing"));

        result.LogError(logger, "loading the order");

        Assert.Null(Assert.Single(logger.Exceptions));
    }
}
