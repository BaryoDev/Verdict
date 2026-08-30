<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/banner-dark.png">
  <img src="assets/banner-light.png" alt="Verdict, zero-allocation Result types for .NET" width="566">
</picture>

# Verdict

[![License: MPL 2.0](https://img.shields.io/badge/License-MPL_2.0-brightgreen.svg)](https://opensource.org/licenses/MPL-2.0)
[![NuGet](https://img.shields.io/nuget/v/Verdict.svg)](https://www.nuget.org/packages/Verdict/)
[![Build](https://github.com/BaryoDev/Verdict/actions/workflows/ci.yml/badge.svg)](https://github.com/BaryoDev/Verdict/actions/workflows/ci.yml)

Error handling as a return value instead of an exception, for .NET. The core
allocates nothing, and the build fails if that stops being true.

```csharp
Result<Order> Load(int id) =>
    _orders.TryGetValue(id, out var order)
        ? order
        : new Error("NOT_FOUND", $"no order {id}");

var response = Load(id)
    .Map(order => order.Total)
    .Match(total => Results.Ok(total), error => error.ToProblem());
```

## The promise, and exactly where it applies

**`Verdict` and `Verdict.Fluent` allocate nothing.** Every operation, on value
types and reference types alike, including composed chains. Measured with
`GC.GetAllocatedBytesForCurrentThread` over 10,000 iterations in Release on
net8.0, and asserted by `tests/Verdict.Allocation.Tests`, so a regression fails a
build rather than a benchmark nobody reads.

| Operation | Bytes |
|---|---:|
| `Success`, `Failure`, `Value`, `Error`, `ValueOr`, `ValueOrDefault` | 0 |
| `Equals`, `GetHashCode`, both implicit conversions | 0 |
| `Bind`, `Tap`, `TapError`, `ToNonGeneric`, `ToGeneric` | 0 |
| `Map`, `Match`, `OnSuccess`, `OnFailure` | 0 |
| `Map` then `Bind` then `Match`, chained | 0 |
| `new Error(code, message)` | 0 |

Under sustained load, which is the number that matters if you are sizing a
service: **1,600,000 composed operations across eight threads with server GC, and
zero collections at gen0, gen1 and gen2.**

**The other packages allocate, on purpose.** They are opt-in because they buy
something that costs something. `MultiResult<T>.Failure` is 48 bytes,
`RichResult.WithSuccess` is 80, JSON round-tripping is about 100. Every one has a
named budget in the same gate.

**`Verdict.Async` has two APIs and only one of them is free.** An `async Task`
method allocates whether or not it waits, so the `Task` overloads cost roughly 96
bytes a step. The `ValueTask` overloads check whether the antecedent has already
completed, which in a request handler it usually has:

| Four steps chained on completed antecedents | Bytes |
|---|---:|
| `Task` API | 480 |
| `ValueTask` API | **0** |

## Install

```bash
dotnet add package Verdict            # core, zero dependencies
dotnet add package Verdict.Fluent     # Map, Match, OnSuccess, OnFailure
```

Then whatever you actually need:

| Package | For | Guide |
|---|---|---|
| `Verdict.Extensions` | multiple errors, combination, validation | [extensions.md](docs/packages/extensions.md) |
| `Verdict.Async` | async composition | [async.md](docs/packages/async.md) |
| `Verdict.Rich` | success messages and error metadata | [rich.md](docs/packages/rich.md) |
| `Verdict.Logging` | `Microsoft.Extensions.Logging` | [logging.md](docs/packages/logging.md) |
| `Verdict.AspNetCore` | ProblemDetails and status codes | [aspnetcore.md](docs/packages/aspnetcore.md) |
| `Verdict.Json` | `System.Text.Json` converters | [json.md](docs/packages/json.md) |

## Quick start

```csharp
using Verdict;
using Verdict.Fluent;

// Implicit both ways, so a method body reads like ordinary code.
Result<int> Parse(string raw) =>
    int.TryParse(raw, out var value) ? value : new Error("INVALID", "not a number");

// Compose without unwrapping.
var doubled = Parse("21").Map(x => x * 2);          // Result<int>

// Unwrap once, at the edge.
var text = doubled.Match(x => $"got {x}", e => $"failed: {e.Code}");
```

Async, with the fast path:

```csharp
using Verdict.Async;

return await LoadAsync(id)
    .AsValueTask()
    .Ensure(order => order.IsOpen, new Error("CLOSED", "the order is closed"))
    .Map(order => order.Total)
    .Match(total => Results.Ok(total), error => error.ToProblem());
```

ASP.NET Core:

```csharp
builder.Services.AddVerdictProblemDetails();

app.MapGet("/orders/{id}", (int id, HttpContext context) =>
    Load(id).ToHttpResult(context));
```

Pass the `HttpContext`. The overloads without one read process-wide statics,
which is wrong if more than one application shares a process.

## The security model

A `Result` is not an internal value. It is built from exceptions and request
bodies, and read into logs and HTTP responses, so it sits on the path between
untrusted input and two sinks that leak. Three defaults follow from that:

**An error carrying an exception is treated as internal.** Its message and its
code are both withheld from an HTTP response unless you turn
`IncludeExceptionDetails` on, and it maps to 500 rather than 400 when nothing
else claims its code. `Error.FromException` uses a constant code rather than the
exception's type name, because the type name identifies your data access stack.

**Messages are neutralised and bounded at construction.** Control characters
become spaces and anything past 4 KB is truncated with a marker, because a
message is where request data gets interpolated and a carriage return in one
forges a line in a plain-text log sink. A clean message is returned by reference,
so this costs nothing on the common path.

**Deserialisation rejects rather than guesses.** A JSON success carrying no
`value` property throws instead of producing a success holding `default(T)`.

Full threat model and what remains your responsibility: [SECURITY.md](SECURITY.md).

## Where Verdict sits

Measured on net8.0 with the same harness, against the libraries people actually
choose between:

| Library | success | failure | chain |
|---|---:|---:|---:|
| **Verdict** | 0 B | **0 B** | 0 B |
| CSharpFunctionalExtensions 3.7.0 | 0 B | **0 B** | 0 B |
| ErrorOr 2.1.1 | 0 B | 88 B | 0 B |
| LightResults 10.0.4 | 0 B | 56 B | 0 B |
| FluentResults 4.0.0 | 232 B | 576 B | 400 B |

**Zero allocation on the success path is table stakes, not a differentiator.**
Four of these five have it. Beating FluentResults measures the gap between a
struct and a class, and every modern competitor closed that gap already. If you
are choosing between Verdict and ErrorOr, that comparison tells you nothing.

Two things do separate them:

**A single-error failure is free here.** `Result<T>` holds one `Error` struct
inline, so a failure is a field assignment. A library holding a list allocates it
even for one error. The failure path is not the rare path: a service under partial
outage runs it on every request, which is when GC pressure is least welcome.

**Nothing else in the field has a completed-antecedent fast path.** Every one of
them allocates on every async composition step, 304 bytes for
CSharpFunctionalExtensions and 912 for ErrorOr across four steps. The `ValueTask`
API here costs nothing.

Against exceptions the case is the ordinary one and does not need a benchmark: a
thrown exception costs microseconds and unwinds the stack, a returned failure
costs nothing and is visible in the signature.

`benchmarks/Verdict.Benchmarks` runs all of this. Timing numbers come from
BenchmarkDotNet on a schedule rather than from a pull request gate, because
timing on a shared runner is noisy enough to fail an innocent change. Allocation
is deterministic, so that is gated on every push instead.

## Runtime support

| Target | Built | Tested |
|---|---|---|
| net8.0 | yes | yes, the whole suite |
| netstandard2.0 | yes | yes, via `tests/Verdict.NetStandard.Tests` |
| .NET 10 runtime | via the net8.0 assets | yes, second CI leg |

Native AOT and trimming: every package is annotated `IsTrimmable` and
`IsAotCompatible`, and `tests/Verdict.Aot.Smoke` is a `PublishAot` console app
that CI publishes and runs on every push. `Verdict.Json` needs its converters
registered explicitly under AOT, through the `JsonTypeInfo` overload rather than
the options one. See [json.md](docs/packages/json.md).

## Thread safety

A `Result<T>` is immutable once created and safe to read from any number of
threads. That is the guarantee.

It is **not** safe to reassign a shared `Result<T>` field while another thread
reads it. The struct exceeds pointer size and the CLR only guarantees atomic
writes up to that, so a reader can observe `IsSuccess` from one write and the
value from another.

```csharp
private readonly Result<Config> _config = LoadConfig();   // safe: published once
private Result<Config> _current;                          // not safe to reassign concurrently
```

Guard a mutable shared slot with a lock, or swap a reference type atomically.

`ErrorCollection` and `MultiResult` have their own rules, because a pooled
collection can be released by code holding a copy. See
[extensions.md](docs/packages/extensions.md).

## Documentation

- [Package guides](docs/README.md), one per package, with what each costs
- [Design decisions](docs/design-decisions.md), including two optimisations that
  were measured and rejected
- [Architect's decision guide](docs/architects_decision_guide.md)
- [Developer quick reference](docs/developer_quick_reference.md)
- [CHANGELOG.md](CHANGELOG.md)

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md). Comment `/take` on an issue to claim it.

The one thing to know before opening a pull request: the allocation gate is not
advisory. If a change makes a zero-allocation operation allocate, the build fails
and the message names the likely cause. If it changes a budgeted number, change
the budget in the same commit so the cost is visible in review.

## Releasing

Bump `<VersionPrefix>` in `Directory.Build.props`, update the changelog, then
push a tag that matches:

```bash
git tag v3.0.0 && git push origin v3.0.0
```

The workflow refuses to publish from anything that is not a matching tag. The
version comes from source rather than a hand-typed input, so what shipped is
recorded in git. Publishing uses NuGet trusted publishing over OIDC, so there is
no long-lived API key.

## Licence

MPL-2.0. See [LICENSE](LICENSE).
