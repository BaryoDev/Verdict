# Verdict documentation

Start with the core package and add only what you need. Nothing here pulls in a
dependency you did not ask for.

```bash
dotnet add package Verdict
```

## Guides

| Guide | Read it when |
| --- | --- |
| [Developer quick reference](developer_quick_reference.md) | You want the API in one page |
| [Architect's decision guide](architects_decision_guide.md) | You are deciding whether to adopt Verdict |
| [How Verdict does it better](how_verdict_does_it_better.md) | You are comparing against FluentResults or similar |
| [Migrating to 2.5](migration-2.4-to-2.5.md) | You are on 2.4 or earlier. **Contains a breaking change** |
| [Thread safety](../README.md#thread-safety) | You share a `Result` between threads |
| [Trimming and Native AOT](../README.md#trimming-and-native-aot) | You publish with `PublishAot` or trimming |

## Packages

Each package is documented on its own page. The core has no dependencies; every
other package depends only on the core unless noted.

| Package | Adds | Guide |
| --- | --- | --- |
| **Verdict** | `Result`, `Result<T>`, `Error`, `Unit`, and the core combinators | [core.md](packages/core.md) |
| **Verdict.Extensions** | Validation, combining many results, exception capture | [extensions.md](packages/extensions.md) |
| **Verdict.Async** | `Task`-returning combinators and timeouts | [async.md](packages/async.md) |
| **Verdict.Fluent** | `Match`, `OnSuccess`, `OnFailure` chaining | [fluent.md](packages/fluent.md) |
| **Verdict.Json** | System.Text.Json converters, AOT-safe registration | [json.md](packages/json.md) |
| **Verdict.Rich** | Success messages and typed error metadata | [rich.md](packages/rich.md) |
| **Verdict.Logging** | `Microsoft.Extensions.Logging` integration | [logging.md](packages/logging.md) |
| **Verdict.AspNetCore** | RFC 7807 ProblemDetails, status code mapping | [aspnetcore.md](packages/aspnetcore.md) |

## Choosing what to install

```text
Verdict                     always
  ├── .Extensions           validating input, collecting multiple errors
  ├── .Async                your methods return Task<Result<T>>
  ├── .Fluent               you prefer Match/OnSuccess over if-statements
  ├── .Json                 Results cross an API boundary
  ├── .Rich                 you need success messages or error metadata
  ├── .Logging              you log results through ILogger
  └── .AspNetCore           you return Results from controllers or minimal APIs
```

A typical web API uses `Verdict`, `.Extensions`, `.Async` and `.AspNetCore`.
A library that returns results to callers usually needs the core alone.

## Internal documents

[`docs/internal/`](internal/) holds security assessments, code reviews and
release summaries from earlier versions. They are kept for history and are **not
current documentation**; several describe 1.0 and 2.1. Nothing there should be
read as describing how the library behaves today.
