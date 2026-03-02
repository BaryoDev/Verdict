using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Verdict.Logging;

/// <summary>
/// Helper class for creating logging-aware result operations.
/// Uses LoggerMessage.Define for high-performance, zero-allocation logging.
/// </summary>
public static class ResultLogger
{
    private static readonly Action<ILogger, string, Exception?> _logStarting =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(10, "OperationStarting"),
            "Starting operation: {OperationName}");

    private static readonly Action<ILogger, string, Exception?> _logStartingAsync =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(11, "AsyncOperationStarting"),
            "Starting async operation: {OperationName}");

    private static readonly Action<ILogger, string, Exception?> _logSucceeded =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(12, "OperationSucceeded"),
            "Operation succeeded: {OperationName}");

    private static readonly Action<ILogger, string, Exception?> _logSucceededAsync =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(13, "AsyncOperationSucceeded"),
            "Async operation succeeded: {OperationName}");

    private static readonly Action<ILogger, string, string, string, Exception?> _logFailed =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(14, "OperationFailed"),
            "Operation failed: {OperationName} - [{ErrorCode}] {ErrorMessage}");

    private static readonly Action<ILogger, string, string, string, Exception?> _logFailedAsync =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(15, "AsyncOperationFailed"),
            "Async operation failed: {OperationName} - [{ErrorCode}] {ErrorMessage}");

    private static readonly Action<ILogger, string, Exception?> _logException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(16, "OperationException"),
            "Operation threw exception: {OperationName}");

    private static readonly Action<ILogger, string, Exception?> _logExceptionAsync =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(17, "AsyncOperationException"),
            "Async operation threw exception: {OperationName}");

    /// <summary>
    /// Creates a result with automatic logging.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The operation name for logging.</param>
    /// <returns>The result of the operation with automatic logging.</returns>
    public static Result<T> Create<T>(
        ILogger logger,
        Func<Result<T>> operation,
        string operationName)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        _logStarting(logger, operationName, null);

        try
        {
            var result = operation();

            if (result.IsSuccess)
            {
                _logSucceeded(logger, operationName, null);
            }
            else
            {
                _logFailed(logger, operationName, result.Error.Code, result.Error.Message, result.Error.Exception);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logException(logger, operationName, ex);
            throw;
        }
    }

    /// <summary>
    /// Wraps an async operation with logging.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="operationName">The operation name for logging.</param>
    /// <returns>The result of the operation with automatic logging.</returns>
    public static async Task<Result<T>> CreateAsync<T>(
        ILogger logger,
        Func<Task<Result<T>>> operation,
        string operationName)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        _logStartingAsync(logger, operationName, null);

        try
        {
            var result = await operation();

            if (result.IsSuccess)
            {
                _logSucceededAsync(logger, operationName, null);
            }
            else
            {
                _logFailedAsync(logger, operationName, result.Error.Code, result.Error.Message, result.Error.Exception);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logExceptionAsync(logger, operationName, ex);
            throw;
        }
    }

    /// <summary>
    /// Creates a non-generic result with automatic logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The operation name for logging.</param>
    /// <returns>The result of the operation with automatic logging.</returns>
    public static Result Create(
        ILogger logger,
        Func<Result> operation,
        string operationName)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        _logStarting(logger, operationName, null);

        try
        {
            var result = operation();

            if (result.IsSuccess)
            {
                _logSucceeded(logger, operationName, null);
            }
            else
            {
                _logFailed(logger, operationName, result.Error.Code, result.Error.Message, result.Error.Exception);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logException(logger, operationName, ex);
            throw;
        }
    }

    /// <summary>
    /// Wraps an async non-generic operation with logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="operationName">The operation name for logging.</param>
    /// <returns>The result of the operation with automatic logging.</returns>
    public static async Task<Result> CreateAsync(
        ILogger logger,
        Func<Task<Result>> operation,
        string operationName)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        _logStartingAsync(logger, operationName, null);

        try
        {
            var result = await operation();

            if (result.IsSuccess)
            {
                _logSucceededAsync(logger, operationName, null);
            }
            else
            {
                _logFailedAsync(logger, operationName, result.Error.Code, result.Error.Message, result.Error.Exception);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logExceptionAsync(logger, operationName, ex);
            throw;
        }
    }
}
