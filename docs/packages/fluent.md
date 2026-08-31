# Verdict.Fluent

Composition over `Result<T>`. Zero dependencies, and every operation allocates
nothing.

```csharp
return Load(id)
    .Map(order => order.Total)
    .Match(total => Results.Ok(total), error => error.ToProblem());
```

| Operation | What it does | Bytes |
|---|---|---:|
| `Map` | transforms the success value | 0 |
| `Match` | unwraps into one value, both branches | 0 |
| `OnSuccess` / `OnFailure` | runs a side effect, passes the result through | 0 |

`Bind` lives in the core package, so a chain can mix the two freely. A three-step
`Map` then `Bind` then `Match` chain measures 0 bytes, and composition does not
accumulate cost.

## The one thing that can allocate

Your lambda, not this package. A lambda that captures nothing is cached by the
compiler and costs nothing per call. A lambda that captures a local allocates a
closure on every call:

```csharp
result.Map(x => x * 2);            // no capture, cached, free
result.Map(x => x * factor);       // captures factor, allocates per call
```

Hoist the multiplier or use a `static` lambda with an explicit argument if that
matters in a hot path.

For the async equivalents see [async.md](async.md).
