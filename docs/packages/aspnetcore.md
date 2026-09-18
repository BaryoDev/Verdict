# Verdict.AspNetCore

Turning results into HTTP responses, as RFC 7807 ProblemDetails.

```csharp
app.MapGet("/orders/{id}", (int id, HttpContext context) =>
    Load(id).ToHttpResult(context));
```

## Pass the HttpContext

The overloads that take an `HttpContext` resolve `IErrorStatusCodeMapper` and
`IVerdictProblemDetailsFactory` from the request's container, which is what
`AddVerdictProblemDetails` registers. The overloads without one read process-wide
statics, so two applications hosted in one process share a single configuration
and the last registration wins. Both work; only one is correct under multi-tenant
hosting.

```csharp
builder.Services.AddVerdictProblemDetails(options =>
{
    options.IncludeErrorMessage = false;
    options.StatusCodeMappings["ORDER_LOCKED"] = 423;
});
```

## What reaches the client, by default

| Option | Default | Controls |
|---|---|---|
| `IncludeErrorMessage` | `true` | whether `Error.Message` becomes `detail` |
| `IncludeErrorCode` | `true` | whether `errorCode` appears in the extensions |
| `IncludeExceptionDetails` | `false` | whether anything about the exception appears |
| `IncludeStackTrace` | `false` | whether the stack frame appears |
| `GenericErrorMessage` | "An unexpected error occurred." | what replaces a suppressed message |

**An error carrying an exception is treated as internal.** Its message and its
code are both withheld unless `IncludeExceptionDetails` is on, whatever the other
options say, and it maps to 500 rather than 400 when nothing else claims its code.

That last part matters twice. Before 3.0 an exception-derived error carried the
exception type as its code, no mapping matched, unmapped codes became 400, and
message suppression only engaged at 500, so `IncludeErrorMessage = false` could
not suppress the one thing it existed for. Server failures also left the process
as 400 and never appeared in 5xx alerting.

## Status codes

`ErrorStatusCodeMapper` maps about thirty conventional codes (`NOT_FOUND` to 404,
`UNAUTHORIZED` to 401, `DUPLICATE_EMAIL` to 409, and so on). Anything unmapped is
400, or 500 if it carries an exception.

Prefer `options.StatusCodeMappings`, which is scoped to a container, over
`ErrorStatusCodeMapper.RegisterMapping`, which is process-wide.

## Validation responses

`CreateFromMultiResult` renders a `MultiResult<T>` as a
`ValidationProblemDetails`. If the errors were already released it says so and
returns a 400, rather than throwing from inside the code that is building the
error response and turning a reported validation failure into an unhandled 500.
