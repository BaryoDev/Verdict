# Verdict

The core package. Zero dependencies, and the only package where the
zero-allocation promise is unconditional.

## What is in it

| Type | Shape | Notes |
|---|---|---|
| `Result<T>` | `readonly struct` | success carries a value, failure carries one `Error` |
| `Result` | `readonly struct` | for operations with no value |
| `Error` | `readonly record struct` | code, message, and an optional exception |
| `Unit` | `readonly struct` | the value type for "nothing" |

```csharp
Result<Order> Load(int id) =>
    _orders.TryGetValue(id, out var order)
        ? order                                        // implicit from T
        : new Error("NOT_FOUND", $"no order {id}");    // implicit from Error
```

## What it costs

Measured with `GC.GetAllocatedBytesForCurrentThread`, 10,000 iterations, Release,
net8.0. Every row is asserted by `tests/Verdict.Allocation.Tests`, so it fails a
build rather than a benchmark if it changes.

| Operation | Bytes |
|---|---:|
| `Success`, `Failure`, both accessors, `Equals`, `GetHashCode` | 0 |
| `Bind`, `Tap`, `TapError`, `ToNonGeneric`, `ToGeneric` | 0 |
| `new Error(code, message)` with a clean message | 0 |
| `result.ToString()` | 48 to 80, it returns a string |
| `error.ToString()` | 64, or 176 with an exception attached |

Under sustained load: 1,600,000 composed operations across eight threads, server
GC, **zero collections at gen0, gen1 and gen2**.

## Things worth knowing

**A failure holds one error.** That is why a failure is free: the error lives in
the struct rather than in a list. Multiple errors are `Verdict.Extensions`, which
is an opt-in package because it allocates.

**Messages are neutralised and bounded.** Control characters become spaces and
anything past `Error.MaxMessageLength` is truncated with a marker, because a
message is where request data gets interpolated and it is written into logs. A
clean message is returned by reference, so the common path still allocates
nothing. The scan costs about 12 ns for a short message and 190 ns for a 4 KB
one, using a cached `SearchValues<char>`.

**`Error.ToString()` never renders the exception.** It prints `[CODE] message`
and the exception's type name. Rendering the exception defeated
`FromException(ex, sanitize: true)` and cost 1,544 bytes.

**`Error.FromException` uses a constant code**, `Error.UnhandledExceptionCode`,
not the exception's type name. The type name identifies your data access stack
and used to reach clients through ProblemDetails. It is still on
`error.Exception` for anyone who wants it.

**Reassigning a shared field is not safe.** `Result<T>` exceeds pointer size, so
a concurrent reader can see half of one write and half of another. Publishing once
and reading from many threads is safe; a mutable shared slot needs a lock or a
reference-type box.
