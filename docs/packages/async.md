# Verdict.Async

Async composition. Two APIs: `ValueTask`, which is free when there is nothing to
wait for, and `Task`, which is not.

## Use the ValueTask one

```csharp
return await LoadAsync(id)
    .AsValueTask()
    .Ensure(order => order.IsOpen, new Error("CLOSED", "the order is closed"))
    .Map(order => order.Total)
    .MatchAsync(
        total => new ValueTask<IResult>(Results.Ok(total)),
        error => new ValueTask<IResult>(error.ToProblem()));
```

Every method checks whether its antecedent has already completed and does the
work inline if so, handing off to a state machine only when it genuinely has to
wait. In a request handler the antecedent usually has completed: a cache hit, a
validation that short-circuits, a repository returning from memory.

| Four steps chained on completed antecedents | Bytes |
|---|---:|
| `Task` API | 480 |
| `ValueTask` API | **0** |

Both numbers are asserted by `tests/Verdict.Allocation.Tests` in the same
harness, so the comparison cannot quietly stop being true.

**The zero is for antecedents that have already completed.** When a step genuinely
has to wait, it hands off to a local `async` function and the state machine is
boxed like any other, so it costs what an `async` method costs. That is the right
trade: work that actually waits on I/O has already paid for far more than a state
machine, and the point of the fast path is the case where nothing is waiting.
`AsValueTask(Task<Result<T>>)` adds nothing itself, because the task it adapts was
allocated by whoever produced it.

## The rule that comes with ValueTask

**A `ValueTask` may be awaited once, and never concurrently.** That is a real
constraint and the reason these are separate overloads rather than a
replacement. If you need to await a result twice, call `AsTask()` first.

`AsValueTask()` starts a pipeline from a `Result<T>` or adapts an existing
`Task<Result<T>>`. Adapting a task adds nothing, because whoever produced it has
already paid for it, and every step after that is free.

## The Task API

Still there, still supported, unchanged in behaviour. It allocates on every step
because an `async Task` method allocates whether or not it waits. `MatchAsync`
was added to it as well, so an async pipeline has a terminal operation on both.

## Operations

`Map`, `MapAsync`, `Bind`, `BindAsync`, `Tap`, `TapAsync`, `TapErrorAsync`,
`Ensure`, `Match`, `MatchAsync`. Cancellation overloads follow the same shape as
the rest of the package: the token is checked before the work and after the await.
