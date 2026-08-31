# Verdict.Extensions

Multiple errors, combination and validation. Depends on `System.Memory` on
netstandard2.0 for `ArrayPool`.

This package allocates. That is the trade: the core is free because a failure
carries one error, and carrying several needs somewhere to put them.

## What is in it

| Type or method | Purpose |
|---|---|
| `MultiResult<T>` | a result whose failure carries several errors |
| `ErrorCollection` | the storage behind it |
| `Combine`, `CombineAll`, `Merge` | fold several results into one |
| `Ensure`, `EnsureAll`, `ValidateAll` | validation |
| `Try`, `TryResult` | turn an exception-throwing call into a result |

## Pooling, and when it applies

`ErrorCollection` uses `ArrayPool<Error>.Shared`, but only between
`ErrorCollection.PoolingThreshold` (8) and `ErrorCollection.PoolingCeiling`
(1024) errors.

| Size | Storage | Disposal |
|---|---|---|
| 8 or fewer | an exact array | nothing to dispose, `Dispose` is a no-op |
| 9 to 1024 | pooled | **read first, then call `DisposeErrors()`** |
| more than 1024 | an exact array | nothing to dispose |

Below the threshold the pool saved eight bytes when the caller got disposal right
and cost four hundred when they did not, and forgetting is the default outcome
for a struct that cannot be used with `using`. Above the ceiling the rented array
is on the large object heap and `ArrayPool.Shared` keeps it for the life of the
process.

Nothing is dropped at any size. The bound is on what goes into the pool, not on
what you get back.

## If you are in the pooled band

```csharp
var result = Validate(request);          // more than eight errors
try
{
    return result.ToProblemDetails();    // read before releasing
}
finally
{
    result.DisposeErrors();
}
```

`ErrorCollection` is a struct, so `using` disposes a copy and not the original.
That is why the method is `DisposeErrors()` and not `Dispose()`.

A combinator such as `Map` or `Bind` gives the derived result its own storage
when the source was pooled, so disposing one cannot invalidate the other. Each
result owns what it holds, and you dispose the one you created.

Calling a combinator on a result whose errors you already released throws
`ObjectDisposedException`, which is the same rule as reading them. `ErrorsDisposed`
answers that without throwing, and anything rendering a failure can ask first.

## What it costs

| Operation | Bytes |
|---|---:|
| `MultiResult<T>.Success`, `Combine`, `Ensure`, `Try` with no throw | 0 |
| `MultiResult<T>.Failure(error)` | 48 |
| `ErrorCollection.Create` for three errors | 96 |
| `Try` on the catch path | 384, mostly the exception |
