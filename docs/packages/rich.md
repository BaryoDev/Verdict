# Verdict.Rich

Success messages and error metadata carried on the result itself.

```csharp
RichResult<Order> result = Load(id)
    .WithSuccess("loaded from cache")
    .WithErrorMetadata("orderId", id);

foreach (var success in result.Successes) { /* ... */ }
```

Metadata lives inside the struct, in an `ImmutableList` and an
`ImmutableDictionary`. Before 2.0 it lived in thread-local storage keyed by the
result, which leaked.

## What it costs

| Operation | Bytes |
|---|---:|
| `AsRich()` | 0 |
| `WithSuccess` | 80, one immutable list node |
| `WithErrorMetadata` | 104, one immutable dictionary node |

Attaching metadata allocates. That is what an opt-in package is for: the core
stays free and you pay only where you asked for something extra.

## Breaking change from 2.0

`WithSuccess()` and `WithErrorMetadata()` return `RichResult<T>`, not
`Result<T>`. `Successes` and `ErrorMetadata` are properties, not methods.
Implicit conversions run in both directions, and converting back to `Result<T>`
drops the metadata.
