using System;

namespace Upshot.Fluent;

/// <summary>
/// Extension methods for Result&lt;T&gt; providing functional composition capabilities.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Pattern matches on the result, executing the appropriate function based on success or failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TOut">The type of the output.</typeparam>
    /// <param name="result">The result to match on.</param>
    /// <param name="onSuccess">Function to execute if the result is successful.</param>
    /// <param name="onFailure">Function to execute if the result is a failure.</param>
    /// <returns>The output of the executed function.</returns>
    public static TOut Match<T, TOut>(
        this Result<T> result,
        Func<T, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    /// <summary>
    /// Maps the success value to a new value using the provided mapper function.
    /// If the result is a failure, the error is propagated.
    /// </summary>
    /// <typeparam name="T">The type of the current success value.</typeparam>
    /// <typeparam name="K">The type of the new success value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="mapper">Function to map the success value.</param>
    /// <returns>A new result with the mapped value or the original error.</returns>
    public static Result<K> Map<T, K>(
        this Result<T> result,
        Func<T, K> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        return result.IsSuccess
            ? Result<K>.Success(mapper(result.Value))
            : Result<K>.Failure(result.Error);
    }

    /// <summary>
    /// Executes an action on the success value if the result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">Action to execute on success.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> OnSuccess<T>(
        this Result<T> result,
        Action<T> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an action on the error if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to operate on.</param>
    /// <param name="action">Action to execute on failure.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result<T> OnFailure<T>(
        this Result<T> result,
        Action<Error> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }
}
