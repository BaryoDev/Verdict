using System;
using System.Threading.Tasks;

namespace Verdict.Extensions;

/// <summary>
/// Combinators for <see cref="MultiResult{T}"/> and <see cref="MultiResult"/>.
/// </summary>
/// <remarks>
/// Validation that accumulates errors produces a <see cref="MultiResult{T}"/>,
/// and without these the chain stopped there: no Map, no Bind, no Match. These
/// keep an accumulated result composable with the same shape as the single-error
/// combinators.
/// <para>
/// A failure carries its <see cref="ErrorCollection"/> through unchanged rather
/// than copying it, so pooled buffers are neither duplicated nor orphaned and
/// disposal semantics are unaffected. The failure branch of
/// <c>Match</c> and <c>OnFailure</c> receives that same collection by value, so
/// nothing allocates.
/// </para>
/// </remarks>
public static class MultiResultCombinators
{
    // ---------------------------------------------------------------- Map --

    /// <summary>
    /// Transforms the value of a successful result. A failure passes through
    /// with every error intact and the mapper is not invoked.
    /// </summary>
    public static MultiResult<TOut> Map<T, TOut>(
        this MultiResult<T> result,
        Func<T, TOut> mapper)
    {
        if (mapper is null) throw new ArgumentNullException(nameof(mapper));

        return result.IsSuccess
            ? MultiResult<TOut>.Success(mapper(result.Value))
            : MultiResult<TOut>.Failure(result.ErrorCollection);
    }

    // --------------------------------------------------------------- Bind --

    /// <summary>
    /// Chains a step that produces its own <see cref="MultiResult{TOut}"/>.
    /// A failure short-circuits, keeping the errors already accumulated.
    /// </summary>
    public static MultiResult<TOut> Bind<T, TOut>(
        this MultiResult<T> result,
        Func<T, MultiResult<TOut>> binder)
    {
        if (binder is null) throw new ArgumentNullException(nameof(binder));

        return result.IsSuccess
            ? binder(result.Value)
            : MultiResult<TOut>.Failure(result.ErrorCollection);
    }

    // -------------------------------------------------------------- Match --

    /// <summary>
    /// Collapses both branches into a single value. The failure branch receives
    /// every accumulated error.
    /// </summary>
    public static TOut Match<T, TOut>(
        this MultiResult<T> result,
        Func<T, TOut> onSuccess,
        Func<ErrorCollection, TOut> onFailure)
    {
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure is null) throw new ArgumentNullException(nameof(onFailure));

        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.ErrorCollection);
    }

    /// <summary>
    /// Collapses both branches of a non-generic result into a single value.
    /// </summary>
    public static TOut Match<TOut>(
        this MultiResult result,
        Func<TOut> onSuccess,
        Func<ErrorCollection, TOut> onFailure)
    {
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure is null) throw new ArgumentNullException(nameof(onFailure));

        return result.IsSuccess ? onSuccess() : onFailure(result.ErrorCollection);
    }

    // ------------------------------------------------------- side effects --

    /// <summary>
    /// Runs an action when successful and returns the original result.
    /// </summary>
    public static MultiResult<T> OnSuccess<T>(this MultiResult<T> result, Action<T> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess) action(result.Value);
        return result;
    }

    /// <summary>
    /// Runs an action with every accumulated error and returns the original result.
    /// </summary>
    public static MultiResult<T> OnFailure<T>(this MultiResult<T> result, Action<ErrorCollection> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        if (result.IsFailure) action(result.ErrorCollection);
        return result;
    }

    /// <summary>
    /// Runs an action when a non-generic result succeeds.
    /// </summary>
    public static MultiResult OnSuccess(this MultiResult result, Action action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        if (result.IsSuccess) action();
        return result;
    }

    /// <summary>
    /// Runs an action with every accumulated error of a non-generic result.
    /// </summary>
    public static MultiResult OnFailure(this MultiResult result, Action<ErrorCollection> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        if (result.IsFailure) action(result.ErrorCollection);
        return result;
    }

    // -------------------------------------------------------------- async --

    /// <summary>
    /// Transforms the value of a successful result asynchronously.
    /// </summary>
    public static async Task<MultiResult<TOut>> MapAsync<T, TOut>(
        this Task<MultiResult<T>> resultTask,
        Func<T, Task<TOut>> mapper)
    {
        if (resultTask is null) throw new ArgumentNullException(nameof(resultTask));
        if (mapper is null) throw new ArgumentNullException(nameof(mapper));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure) return MultiResult<TOut>.Failure(result.ErrorCollection);

        return MultiResult<TOut>.Success(await mapper(result.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Chains an asynchronous step that produces its own result.
    /// </summary>
    public static async Task<MultiResult<TOut>> BindAsync<T, TOut>(
        this Task<MultiResult<T>> resultTask,
        Func<T, Task<MultiResult<TOut>>> binder)
    {
        if (resultTask is null) throw new ArgumentNullException(nameof(resultTask));
        if (binder is null) throw new ArgumentNullException(nameof(binder));

        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure
            ? MultiResult<TOut>.Failure(result.ErrorCollection)
            : await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an asynchronous action when successful and returns the original result.
    /// </summary>
    public static async Task<MultiResult<T>> OnSuccessAsync<T>(
        this Task<MultiResult<T>> resultTask,
        Func<T, Task> action)
    {
        if (resultTask is null) throw new ArgumentNullException(nameof(resultTask));
        if (action is null) throw new ArgumentNullException(nameof(action));

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess) await action(result.Value).ConfigureAwait(false);
        return result;
    }
}
