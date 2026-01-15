using System;
using System.Threading;
using System.Threading.Tasks;

namespace Verdict.Async;

/// <summary>
/// Async extension methods for Result types.
/// </summary>
public static class AsyncResultExtensions
{
    // ==================== MapAsync ====================

    /// <summary>
    /// Maps the success value to a new value asynchronously.
    /// </summary>
    public static async Task<Result<K>> MapAsync<T, K>(
        this Task<Result<T>> resultTask,
        Func<T, Task<K>> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<K>.Failure(result.Error);

        var mappedValue = await mapper(result.Value).ConfigureAwait(false);
        return Result<K>.Success(mappedValue);
    }

    /// <summary>
    /// Maps the success value to a new value asynchronously with cancellation support.
    /// </summary>
    public static async Task<Result<K>> MapAsync<T, K>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task<K>> mapper,
        CancellationToken cancellationToken)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<K>.Failure(result.Error);

        cancellationToken.ThrowIfCancellationRequested();
        var mappedValue = await mapper(result.Value, cancellationToken).ConfigureAwait(false);
        return Result<K>.Success(mappedValue);
    }

    /// <summary>
    /// Maps the success value to a new value synchronously from an async result.
    /// </summary>
    public static async Task<Result<K>> Map<T, K>(
        this Task<Result<T>> resultTask,
        Func<T, K> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result<K>.Success(mapper(result.Value))
            : Result<K>.Failure(result.Error);
    }

    // ==================== BindAsync ====================

    /// <summary>
    /// Binds the result to another async result-producing function.
    /// </summary>
    public static async Task<Result<K>> BindAsync<T, K>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<K>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<K>.Failure(result.Error);

        return await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds the result to another async result-producing function with cancellation support.
    /// </summary>
    public static async Task<Result<K>> BindAsync<T, K>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task<Result<K>>> binder,
        CancellationToken cancellationToken)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<K>.Failure(result.Error);

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(result.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds the result to a synchronous result-producing function.
    /// </summary>
    public static async Task<Result<K>> Bind<T, K>(
        this Task<Result<T>> resultTask,
        Func<T, Result<K>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? binder(result.Value)
            : Result<K>.Failure(result.Error);
    }

    // ==================== TapAsync ====================

    /// <summary>
    /// Executes an async side effect on the success value.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Executes an async side effect on the success value with cancellation support.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(result.Value, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Executes a synchronous side effect on the success value.
    /// </summary>
    public static async Task<Result<T>> Tap<T>(
        this Task<Result<T>> resultTask,
        Action<T> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an async side effect on the error.
    /// </summary>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            await action(result.Error).ConfigureAwait(false);
        }

        return result;
    }

    // ==================== EnsureAsync ====================

    /// <summary>
    /// Ensures a condition is met asynchronously.
    /// </summary>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        Error error)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return result;

        var isValid = await predicate(result.Value).ConfigureAwait(false);
        return isValid
            ? result
            : Result<T>.Failure(error);
    }

    /// <summary>
    /// Ensures a condition is met asynchronously with cancellation support.
    /// </summary>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task<bool>> predicate,
        Error error,
        CancellationToken cancellationToken)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return result;

        cancellationToken.ThrowIfCancellationRequested();
        var isValid = await predicate(result.Value, cancellationToken).ConfigureAwait(false);
        return isValid
            ? result
            : Result<T>.Failure(error);
    }

    /// <summary>
    /// Ensures a condition is met asynchronously with code and message.
    /// </summary>
    public static Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        string code,
        string message)
    {
        return resultTask.EnsureAsync(predicate, new Error(code, message));
    }

    /// <summary>
    /// Ensures a condition is met asynchronously with code, message, and cancellation support.
    /// </summary>
    public static Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task<bool>> predicate,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        return resultTask.EnsureAsync(predicate, new Error(code, message), cancellationToken);
    }

    /// <summary>
    /// Ensures a condition is met asynchronously with a dynamically generated error.
    /// Useful when the error message needs to include information from the value being validated.
    /// </summary>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        Func<T, Error> errorFactory)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (errorFactory == null) throw new ArgumentNullException(nameof(errorFactory));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return result;

        var isValid = await predicate(result.Value).ConfigureAwait(false);
        return isValid
            ? result
            : Result<T>.Failure(errorFactory(result.Value));
    }

    /// <summary>
    /// Ensures a condition is met asynchronously with a dynamically generated error and cancellation support.
    /// </summary>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task<bool>> predicate,
        Func<T, Error> errorFactory,
        CancellationToken cancellationToken)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (errorFactory == null) throw new ArgumentNullException(nameof(errorFactory));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return result;

        cancellationToken.ThrowIfCancellationRequested();
        var isValid = await predicate(result.Value, cancellationToken).ConfigureAwait(false);
        return isValid
            ? result
            : Result<T>.Failure(errorFactory(result.Value));
    }

    // ==================== Timeout Support ====================

    /// <summary>
    /// Applies a timeout to the result task. Returns a failure with the specified error if the timeout expires.
    /// </summary>
    public static async Task<Result<T>> WithTimeout<T>(
        this Task<Result<T>> resultTask,
        TimeSpan timeout,
        Error timeoutError)
    {
        using var cts = new CancellationTokenSource();
        var delayTask = Task.Delay(timeout, cts.Token);
        var completedTask = await Task.WhenAny(resultTask, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            return Result<T>.Failure(timeoutError);
        }

        cts.Cancel();
        return await resultTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Applies a timeout to the result task. Returns a failure with the specified code and message if the timeout expires.
    /// </summary>
    public static Task<Result<T>> WithTimeout<T>(
        this Task<Result<T>> resultTask,
        TimeSpan timeout,
        string code,
        string message)
    {
        return resultTask.WithTimeout(timeout, new Error(code, message));
    }

    /// <summary>
    /// Applies a timeout to the result task using a linked CancellationToken.
    /// The operation will be cancelled if either the timeout expires or the provided token is cancelled.
    /// </summary>
    public static async Task<Result<T>> WithTimeout<T>(
        this Task<Result<T>> resultTask,
        TimeSpan timeout,
        Error timeoutError,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await resultTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<T>.Failure(timeoutError);
        }
    }

    // ==================== Non-Generic Result Extensions ====================

    /// <summary>
    /// Binds a non-generic result to an async result-producing function.
    /// </summary>
    public static async Task<Result> BindAsync(
        this Task<Result> resultTask,
        Func<Task<Result>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        return await binder().ConfigureAwait(false);
    }

    /// <summary>
    /// Binds a non-generic result to an async result-producing function with cancellation support.
    /// </summary>
    public static async Task<Result> BindAsync(
        this Task<Result> resultTask,
        Func<CancellationToken, Task<Result>> binder,
        CancellationToken cancellationToken)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds a non-generic result to an async generic result-producing function.
    /// </summary>
    public static async Task<Result<T>> BindAsync<T>(
        this Task<Result> resultTask,
        Func<Task<Result<T>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<T>.Failure(result.Error);

        return await binder().ConfigureAwait(false);
    }

    /// <summary>
    /// Binds a non-generic result to an async generic result-producing function with cancellation support.
    /// </summary>
    public static async Task<Result<T>> BindAsync<T>(
        this Task<Result> resultTask,
        Func<CancellationToken, Task<Result<T>>> binder,
        CancellationToken cancellationToken)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
            return Result<T>.Failure(result.Error);

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an async side effect on a non-generic result.
    /// </summary>
    public static async Task<Result> TapAsync(
        this Task<Result> resultTask,
        Func<Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Executes an async side effect on a non-generic result with cancellation support.
    /// </summary>
    public static async Task<Result> TapAsync(
        this Task<Result> resultTask,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Applies a timeout to a non-generic result task.
    /// </summary>
    public static async Task<Result> WithTimeout(
        this Task<Result> resultTask,
        TimeSpan timeout,
        Error timeoutError)
    {
        using var cts = new CancellationTokenSource();
        var delayTask = Task.Delay(timeout, cts.Token);
        var completedTask = await Task.WhenAny(resultTask, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            return Result.Failure(timeoutError);
        }

        cts.Cancel();
        return await resultTask.ConfigureAwait(false);
    }
}
