using System;
using Microsoft.Extensions.Logging;

namespace Verdict.Logging;

/// <summary>
/// Logging extensions for Result types.
/// </summary>
public static class ResultLoggingExtensions
{
    // High-performance logging delegates using LoggerMessage.Define
    private static readonly Action<ILogger, string, Exception?> _logSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "ResultSuccess"),
            "{Message}");

    private static readonly Action<ILogger, string, string, string, Exception?> _logFailure =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(2, "ResultFailure"),
            "{Message} - [{ErrorCode}] {ErrorMessage}");

    private static readonly Action<ILogger, string, bool, string?, string?, Exception?> _logStructured =
        LoggerMessage.Define<string, bool, string?, string?>(
            LogLevel.Information,
            new EventId(3, "ResultStructured"),
            "{Message} - IsSuccess: {IsSuccess}, ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}");

    /// <summary>
    /// Logs the result (success or failure) using the provided logger.
    /// Success = Information level, Failure = Error level.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> Log<T>(
        this Result<T> result,
        ILogger logger,
        string message)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        if (result.IsSuccess)
        {
            _logSuccess(logger, message, null);
        }
        else
        {
            _logFailure(logger, message, result.Error.Code, result.Error.Message, result.Error.Exception);
        }

        return result;
    }

    /// <summary>
    /// Logs the result with custom log levels for success and failure.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="successLevel">Log level for success.</param>
    /// <param name="failureLevel">Log level for failure.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> Log<T>(
        this Result<T> result,
        ILogger logger,
        string message,
        LogLevel successLevel,
        LogLevel failureLevel)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        if (result.IsSuccess)
        {
            logger.Log(successLevel, "{Message}", message);
        }
        else
        {
            logger.Log(failureLevel, "{Message} - [{ErrorCode}] {ErrorMessage}",
                message, result.Error.Code, result.Error.Message);
        }

        return result;
    }

    /// <summary>
    /// Logs only on success.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="level">Log level (default: Information).</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> LogSuccess<T>(
        this Result<T> result,
        ILogger logger,
        string message,
        LogLevel level = LogLevel.Information)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (result.IsFailure) return result;

        logger.Log(level, "{Message}", message);
        return result;
    }

    /// <summary>
    /// Logs only on failure.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="level">Log level (default: Error).</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> LogError<T>(
        this Result<T> result,
        ILogger logger,
        string message,
        LogLevel level = LogLevel.Error)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (result.IsSuccess) return result;

        logger.Log(level, "{Message} - [{ErrorCode}] {ErrorMessage}",
            message, result.Error.Code, result.Error.Message);
        return result;
    }

    /// <summary>
    /// Logs with structured logging support (include error code, message in log properties).
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> LogStructured<T>(
        this Result<T> result,
        ILogger logger,
        string messageTemplate)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        _logStructured(
            logger,
            messageTemplate,
            result.IsSuccess,
            result.IsFailure ? result.Error.Code : null,
            result.IsFailure ? result.Error.Message : null,
            result.IsFailure ? result.Error.Exception : null);

        return result;
    }

    /// <summary>
    /// Logs the non-generic result (success or failure).
    /// </summary>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result Log(
        this Result result,
        ILogger logger,
        string message)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        if (result.IsSuccess)
        {
            _logSuccess(logger, message, null);
        }
        else
        {
            _logFailure(logger, message, result.Error.Code, result.Error.Message, result.Error.Exception);
        }

        return result;
    }

    /// <summary>
    /// Logs only on success for non-generic result.
    /// </summary>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="level">Log level (default: Information).</param>
    /// <returns>The original result for chaining.</returns>
    public static Result LogSuccess(
        this Result result,
        ILogger logger,
        string message,
        LogLevel level = LogLevel.Information)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (result.IsFailure) return result;

        logger.Log(level, "{Message}", message);
        return result;
    }

    /// <summary>
    /// Logs only on failure for non-generic result.
    /// </summary>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="level">Log level (default: Error).</param>
    /// <returns>The original result for chaining.</returns>
    public static Result LogError(
        this Result result,
        ILogger logger,
        string message,
        LogLevel level = LogLevel.Error)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (result.IsSuccess) return result;

        logger.Log(level, "{Message} - [{ErrorCode}] {ErrorMessage}",
            message, result.Error.Code, result.Error.Message);
        return result;
    }
}
