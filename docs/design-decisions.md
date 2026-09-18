# Design decisions

Why the library is shaped the way it is, including the things that were tried and
did not work. A negative result only stops being re-derived if it is written down
where the person about to re-derive it will look.

## Why `readonly struct`

A `Result<T>` that is a class allocates on every call, and the whole argument for
this library is that it does not. A struct also gives immutability and copy
semantics for free, so a result cannot be aliased and mutated behind your back.

The cost is that the type exceeds pointer size, so reassigning a shared field is
not atomic. That is documented rather than papered over.

## Why the Fluent extensions are a separate package

`Map`, `Match` and the rest are opinions about how you compose. Not everybody
wants them, and the core should not carry weight for a caller who does not use
it. The same reasoning puts multiple errors, metadata, logging, JSON and HTTP in
their own packages: the core stays free, and you pay only where you asked for
something.

## Why a failure carries one error

`Result<T>` holds a single `Error` struct inline, so constructing a failure is a
field assignment and costs nothing. A library that holds a list allocates the list
even for one error, which is why ErrorOr costs 88 bytes on the failure path and
this costs none.

Several errors need somewhere to put them, so `MultiResult<T>` is in
`Verdict.Extensions` and does allocate. Moving it into the core would make every
failure pay for a case most callers never hit.

## Why `ErrorCollection` only pools in a band

Measured on eight threads before this changed:

| Path | Bytes per operation | Gen0 collections |
|---|---:|---:|
| pooled, disposed correctly | 88 | 2 |
| an ordinary array | 96 | not separately measured |
| pooled, dispose forgotten | 496 | 12 |

The pool bought eight bytes when the caller got it right and cost four hundred
when they did not, and forgetting is the default outcome: `ErrorCollection` is a
struct so `using` does not work, the method is `DisposeErrors()` on an object that
usually arrives from a combinator, and nothing failed when it was skipped.

So it pools only for **9 to 1024 errors**. At **8 or fewer**, and at **more than
1024**, the collection owns an exact array: there is no rental, `Dispose` is a
no-op, and a copy cannot be invalidated by a sibling. The count is what decides
whether `DisposeErrors()` is required, which is why both bounds are public
constants rather than implementation details.

## Two optimisations that were measured and rejected

Both of these look obviously available in a struct-based library, and neither is.
30,000,000 two-step chains, best of five runs, Release, net8.0, Apple M1:

| Variant | ns per chain | vs shipped |
|---|---:|---:|
| shipped: by-value `this Result<T>`, no hint | 2.317 | 1.00x |
| `in Result<T>` parameters | 2.390 | 0.97x |
| `[MethodImpl(AggressiveInlining)]` | 2.410 | 0.96x |

Both are marginally slower and neither difference is outside run-to-run noise.
The JIT already inlines methods this small and already enregisters a struct this
size, so the attribute adds nothing and `in` adds an indirection the JIT then has
to remove again.

**The core is at the floor.** 2.3 nanoseconds for a two-step composed chain, zero
bytes, and no collection at any generation under sustained eight-thread load.
There is no micro-optimisation left in the core worth a release, and the wins are
elsewhere: the async fast path, `Error.ToString`, and the pooled error path, all
of which were taken.

## Why the async package has two APIs

An `async Task` method allocates a state machine and a task whether or not it
waits. In a request handler it usually does not need to wait: a cache hit, a
validation that short-circuits, a repository returning from memory.

The `ValueTask` overloads check whether the antecedent has completed and do the
work inline if so. Four steps chained over completed antecedents cost 480 bytes on
the `Task` API and 0 on this one, and both numbers are asserted by the allocation
gate in the same harness.

`ValueTask` may only be awaited once, and never concurrently, which is a real
constraint on the caller. That is why it is a second API rather than a
replacement, and why `AsTask()` exists.

## Why the allocation gate is a table rather than a benchmark

A benchmark is read by a person, occasionally. A test fails a build. The gate
holds 41 operations that must allocate nothing and 15 with a named byte budget,
and changing a budget is a deliberate edit that shows up in review.

Two things about it are load bearing. Delegates and inputs are created before the
baseline is read, so a closure the caller had to allocate is not charged to the
library. And it is checked against a deliberate mutation, because the first
mutation tried, boxing a `bool`, was elided by the JIT and proved nothing: a
gate has to be shown to fail before it can be trusted to pass.
