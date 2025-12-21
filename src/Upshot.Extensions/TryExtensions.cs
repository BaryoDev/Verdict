using System;

namespace Upshot.Extensions;

/// <summary>
/// Extension methods for try/catch helpers.
/// </summary>
public static class TryExtensions
{
    /// <summary>
    /// Executes a function and returns a Result, catching any exceptions.
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
                : Error.FromException(ex);
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// Executes an action and returns a Result, catching any exceptions.
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
                : Error.FromException(ex);
            return Result.Failure(error);
        }
    }

    /// <summary>
    /// Wraps a Result-returning function in a try/catch.
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
                : Error.FromException(ex);
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// Wraps a non-generic Result-returning function in a try/catch.
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
                : Error.FromException(ex);
            return Result.Failure(error);
        }
    }
}
