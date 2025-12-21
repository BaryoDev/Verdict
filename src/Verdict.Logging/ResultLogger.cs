using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Verdict.Logging;

/// <summary>
/// Helper class for creating logging-aware result operations.
/// </summary>
public static class ResultLogger
{
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
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (logger == null) return operation();

        logger.LogDebug("Starting operation: {OperationName}", operationName);

        try
        {
            var result = operation();

            if (result.IsSuccess)
            {
                logger.LogInformation("Operation succeeded: {OperationName}", operationName);
            }
            else
            {
                logger.LogError("Operation failed: {OperationName} - [{ErrorCode}] {ErrorMessage}",
                    operationName, result.Error.Code, result.Error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation threw exception: {OperationName}", operationName);
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
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (logger == null) return await operation();

        logger.LogDebug("Starting async operation: {OperationName}", operationName);

        try
        {
            var result = await operation();

            if (result.IsSuccess)
            {
                logger.LogInformation("Async operation succeeded: {OperationName}", operationName);
            }
            else
            {
                logger.LogError("Async operation failed: {OperationName} - [{ErrorCode}] {ErrorMessage}",
                    operationName, result.Error.Code, result.Error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Async operation threw exception: {OperationName}", operationName);
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
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (logger == null) return operation();

        logger.LogDebug("Starting operation: {OperationName}", operationName);

        try
        {
            var result = operation();

            if (result.IsSuccess)
            {
                logger.LogInformation("Operation succeeded: {OperationName}", operationName);
            }
            else
            {
                logger.LogError("Operation failed: {OperationName} - [{ErrorCode}] {ErrorMessage}",
                    operationName, result.Error.Code, result.Error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation threw exception: {OperationName}", operationName);
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
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (logger == null) return await operation();

        logger.LogDebug("Starting async operation: {OperationName}", operationName);

        try
        {
            var result = await operation();

            if (result.IsSuccess)
            {
                logger.LogInformation("Async operation succeeded: {OperationName}", operationName);
            }
            else
            {
                logger.LogError("Async operation failed: {OperationName} - [{ErrorCode}] {ErrorMessage}",
                    operationName, result.Error.Code, result.Error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Async operation threw exception: {OperationName}", operationName);
            throw;
        }
    }
}
