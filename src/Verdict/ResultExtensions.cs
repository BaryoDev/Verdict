using System;

namespace Verdict;

/// <summary>
/// Core extension methods for Result types.
/// These extensions maintain zero-dependency and focus on essential operations.
/// </summary>
public static class ResultExtensions
{
    // ==================== Result<T> Extensions ====================

    /// <summary>
    /// Binds the result to another result-producing function (flatMap/chain).
    /// Implements railway-oriented programming.
    /// </summary>
    public static Result<K> Bind<T, K>(this Result<T> result, Func<T, Result<K>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        return result.IsSuccess
            ? binder(result.Value)
            : Result<K>.Failure(result.Error);
    }

    /// <summary>
    /// Executes a side effect on the success value without modifying the result.
    /// Useful for logging, debugging, or other side effects in a chain.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes a side effect on the error without modifying the result.
    /// Useful for logging errors in a chain.
    /// </summary>
    public static Result<T> TapError<T>(this Result<T> result, Action<Error> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    /// <summary>
    /// Converts a Result<T> to a non-generic Result, discarding the value.
    /// </summary>
    public static Result ToNonGeneric<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error);
    }

    // ==================== Result (Non-Generic) Extensions ====================

    /// <summary>
    /// Binds the result to another result-producing function.
    /// </summary>
    public static Result Bind(this Result result, Func<Result> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        return result.IsSuccess
            ? binder()
            : Result.Failure(result.Error);
    }

    /// <summary>
    /// Binds the result to a result-producing function that returns a value.
    /// </summary>
    public static Result<T> Bind<T>(this Result result, Func<Result<T>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        return result.IsSuccess
            ? binder()
            : Result<T>.Failure(result.Error);
    }

    /// <summary>
    /// Executes a side effect without modifying the result.
    /// </summary>
    public static Result Tap(this Result result, Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Executes a side effect on the error without modifying the result.
    /// </summary>
    public static Result TapError(this Result result, Action<Error> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    /// <summary>
    /// Converts a non-generic Result to Result<Unit>.
    /// </summary>
    public static Result<Unit> ToGeneric(this Result result)
    {
        return result.IsSuccess
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure(result.Error);
    }
}
