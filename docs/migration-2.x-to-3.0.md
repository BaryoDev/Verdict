# Migrating from 2.x to 3.0

Most code needs no change. The breaking changes are concentrated in three places:
what an error carries, what reaches an HTTP client, and what the JSON converters
accept.

Read the two marked **action required** first. The rest are behaviour changes you
should know about but that need no edit.

## Action required

### 1. Anything switching on the code of an exception-derived error

`Error.FromException` used to set `Code` to the exception's type name. It now uses
`Error.UnhandledExceptionCode`, the constant `"UNHANDLED_EXCEPTION"`.

```csharp
// Before: this worked
if (error.Code == "SqlException") { /* ... */ }

// After: use the exception itself
if (error.Exception is SqlException) { /* ... */ }
```

The type name identified your data access stack and reached clients through the
ProblemDetails `errorCode` extension, which is on by default, while the option
that exists to hide exception detail is off by default. One field was smuggling
the other.

If you registered a status code mapping keyed on a type name, move it:

```csharp
// Before
ErrorStatusCodeMapper.RegisterMapping("TimeoutException", 504);

// After: give the error a code you chose
return Error.FromException(ex, "GATEWAY_TIMEOUT", sanitize: true);
```

### 2. Anything parsing `Error.ToString()`

The format changed from the compiler-generated record output to
`[CODE] message`, with the exception's type name appended when one is attached.

```text
Before: Error { Code = NOT_FOUND, Message = missing, Exception =  }
After:  [NOT_FOUND] missing
```

The old form rendered the exception, so an error built with `sanitize: true`
handed the original message straight back the moment anything logged it, and cost
1,544 bytes doing it. If you were parsing that string, read `Code`, `Message` and
`Exception` instead.

## Behaviour changes, no edit needed

### Error messages are cleaned and bounded

Control characters become spaces and anything past `Error.MaxMessageLength`
(4096) is truncated with `Error.TruncationMarker`. A message with none of those
is returned by reference and costs nothing extra.

This is why: a message is where a username or a request value gets interpolated,
and a carriage return in one forges a line in a plain-text log sink.

Cost, if you build errors with very large messages on a hot path: about 12 ns for
a short message and 190 ns for a 4 KB one, against a field assignment before, and
still zero bytes. The scan uses a cached `SearchValues<char>` on net8.0.

### `IncludeErrorMessage = false` now works everywhere

Suppression used to key on `statusCode >= 500`, so a 4xx kept its message
whatever the option said. An error built from an exception maps to 400 unless you
map it, so the option could never suppress the messages it existed for.

If you relied on 4xx keeping its message while the option was off, set
`IncludeErrorMessage = true` and rely on the exception rule below instead.

### An error carrying an exception is treated as internal

- It maps to **500** rather than 400 when nothing else claims its code.
- Its message and its code are both withheld from the response unless
  `IncludeExceptionDetails` is on.

If you were mapping exception-derived errors to 4xx deliberately, give them a code
and a mapping.

### `GenericServerErrorMessage` is renamed

Now `GenericErrorMessage`, because it applies to any suppressed message rather
than only server errors. The old name still works and forwards to the new one, so
nothing breaks today, but it is marked obsolete.

### JSON rejects a success with no value

```json
{"isSuccess":true}
```

used to produce a success holding `default(T)`. For `Result<Uri>` that is a
success carrying null, from a payload your process did not write. It now throws
`JsonException`. An explicit `"value":null` is still accepted, because the payload
asked for it.

### A derived `MultiResult` owns its own errors

`var b = a.Map(f)` used to give `b` the same pooled array as `a`, so
`a.DisposeErrors()` left `b.ErrorCount` throwing. Each derived result now has its
own copy when there was anything to copy.

Calling a combinator on a result whose errors you already released now throws
`ObjectDisposedException` instead of quietly producing a broken result. That is
the same rule as reading them: released errors cannot be read. Ask
`ErrorsDisposed` if you are not sure.

### `ErrorCollection` pools less

Pooling now applies only between `PoolingThreshold` (8) and `PoolingCeiling`
(1024) errors. Below the threshold there is no rental, so `Dispose` is a no-op and
you cannot invalidate a copy. Above the ceiling the array would sit on the large
object heap and the pool would keep it for the life of the process.

Nothing is dropped at any size. If you were calling `DisposeErrors()` you should
keep calling it: it is correct at every size and required in the middle band.

## New things worth adopting

### The `ValueTask` async API

Four steps chained over already-completed antecedents cost 480 bytes on the
`Task` API and nothing on this one.

```csharp
// Before
var result = await LoadAsync(id).Map(ToDto);
return result.Match(Results.Ok, ToProblem);

// After
return await LoadAsync(id)
    .AsValueTask()
    .Map(ToDto)
    .Match(Results.Ok, ToProblem);
```

A `ValueTask` may only be awaited once and never concurrently. `AsTask()` gives
you a `Task` back if you need one. The `Task` API is unchanged and still
supported.

`MatchAsync` now exists on both, so a pipeline no longer has to be awaited into a
local before it can be unwrapped.

### Pass the `HttpContext`

```csharp
// Before: reads process-wide statics
app.MapGet("/orders/{id}", (int id) => Load(id).ToHttpResult());

// After: reads the configuration registered in this request's container
app.MapGet("/orders/{id}", (int id, HttpContext context) => Load(id).ToHttpResult(context));
```

`AddVerdictProblemDetails` has registered `IErrorStatusCodeMapper` and
`IVerdictProblemDetailsFactory` since 2.7.0 and nothing resolved them, so two
applications in one process shared a single configuration. These overloads reach
them. The old ones still work and still read the statics.

## If something breaks that is not listed here

Open an issue. The public API of every package is captured in
`tests/Verdict.ApiApproval.Tests/ApprovedApi`, so a change that is not in this
document is a change nobody intended.
