using System;

namespace Verdict.Extensions;

/// <summary>
/// Extension methods for try/catch helpers.
/// </summary>
/// <remarks>
/// Every overload catches every exception, including
/// <see cref="OperationCanceledException"/>. A cancelled action comes back as a
/// failure (code <c>UNHANDLED_EXCEPTION</c> unless an errorFactory says otherwise),
/// not as a thrown <see cref="OperationCanceledException"/>. The async helpers in
/// Verdict.Async let caller cancellation propagate; these synchronous ones do not.
/// Pass an errorFactory that maps <see cref="OperationCanceledException"/> to your
/// own code if you need to tell cancellation from a fault.
/// </remarks>
public static class TryExtensions
{
    /// <summary>
    /// Executes a function and returns a Result, catching any exceptions.
    /// By default, creates an error with the exception message sanitized.
    /// Pass a custom errorFactory to control error creation.
    /// </summary>
    public static Result<T> Try<T>(
        Func<T> action,
        Func<Exception, Error>? errorFactory = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        try
        {
            return Result<T>.Success(action());
        }
        catch (Exception ex)
        {
            var error = errorFactory != null
                ? errorFactory(ex)
                : Error.FromException(ex, sanitize: true);
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// Executes an action and returns a Result, catching any exceptions.
    /// By default, creates an error with the exception message sanitized.
    /// Pass a custom errorFactory to control error creation.
    /// </summary>
    public static Result Try(
        Action action,
        Func<Exception, Error>? errorFactory = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        try
        {
            action();
            return Result.Success();
        }
        catch (Exception ex)
        {
            var error = errorFactory != null
                ? errorFactory(ex)
                : Error.FromException(ex, sanitize: true);
            return Result.Failure(error);
        }
    }

    /// <summary>
    /// Wraps a Result-returning function in a try/catch.
    /// By default, creates an error with the exception message sanitized.
    /// Pass a custom errorFactory to control error creation.
    /// </summary>
    public static Result<T> TryResult<T>(
        Func<Result<T>> action,
        Func<Exception, Error>? errorFactory = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        try
        {
            return action();
        }
        catch (Exception ex)
        {
            var error = errorFactory != null
                ? errorFactory(ex)
                : Error.FromException(ex, sanitize: true);
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// Wraps a non-generic Result-returning function in a try/catch.
    /// By default, creates an error with the exception message sanitized.
    /// Pass a custom errorFactory to control error creation.
    /// </summary>
    public static Result TryResult(
        Func<Result> action,
        Func<Exception, Error>? errorFactory = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        try
        {
            return action();
        }
        catch (Exception ex)
        {
            var error = errorFactory != null
                ? errorFactory(ex)
                : Error.FromException(ex, sanitize: true);
            return Result.Failure(error);
        }
    }
}
