# Verdict.Logging

`Microsoft.Extensions.Logging` integration.

```csharp
return Load(id)
    .LogError(logger, "loading the order")
    .Map(order => order.Total);
```

| Method | When it logs |
|---|---|
| `Log` | always, at one level for success and another for failure |
| `LogSuccess` | on success only |
| `LogError` | on failure only |
| `LogStructured` | with your own event and property names |
| `ResultLogger.Execute` | wraps an operation and logs start, outcome and any throw |

## The exception survives

Every failure path passes `Error.Exception` to the logger's exception parameter,
so the stack trace reaches the sink. Until 3.0 the extension methods used an
overload with no exception parameter and dropped it silently, while
`ResultLogger` kept it, so the version most callers reached was the lossy one.

## Templates are constants

Every message template in this package is a compile-time constant and your data
is passed as arguments, so nothing here allows format-string injection into a
log. Message content is neutralised in the core package, at construction, so a
carriage return cannot forge a line in a plain-text sink either.

## What it costs

Logging allocates, in the logging framework rather than here. If a call sits in a
path that must stay free, guard it with `logger.IsEnabled(level)` or keep it off
the success path.
