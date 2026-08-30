# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.0.0] - 2026-08-31

The release where the zero-allocation claim became something the build enforces
rather than something the README asserts, and where the defaults that leak stopped
leaking.

### Breaking

- **`Error.FromException` no longer sets `Code` to the exception's type name.** It
  uses `Error.UnhandledExceptionCode`. The type name identifies the data access
  stack and often the vendor, and it reached clients through the ProblemDetails
  `errorCode` extension, which is on by default, while the option that exists to
  hide exception detail is off by default. The type is still on
  `error.Exception`. (#46)
- **`Error.ToString()` no longer renders the exception.** It prints
  `[CODE] message`, with the exception's type name appended when one is attached.
  The generated record-struct `ToString` printed every property, so a sanitised
  error handed the original message back the moment anything logged it, and cost
  1,544 bytes doing it. (#19)
- **Error messages are neutralised and bounded at construction.** Control
  characters become spaces, and anything past `Error.MaxMessageLength` (4096) is
  truncated with `Error.TruncationMarker`. A clean message is returned by
  reference, so the hot path still allocates nothing. (#47, #48)
- **`IncludeErrorMessage = false` now suppresses at any status code**, not only
  5xx, and an error carrying an exception has both its message and its code
  withheld unless `IncludeExceptionDetails` is on. (#45, #46)
- **An error carrying an exception maps to 500** rather than 400 when nothing
  else claims its code. Server failures used to leave as client errors and never
  reach 5xx alerting. (#45)
- **`VerdictProblemDetailsOptions.GenericServerErrorMessage` is renamed
  `GenericErrorMessage`.** The old name still works and forwards. (#45)
- **A JSON success carrying no `value` property is rejected** instead of becoming
  a success holding `default(T)`. An explicit `"value":null` is still accepted.
  (#27)
- **A `MultiResult` derived from another owns its own errors.** Combinators used
  to pass the collection through, so disposing either broke the other. (#28)

### Added

- **`ValueTask` overloads throughout `Verdict.Async`,** with a synchronous fast
  path when the antecedent has already completed. A four-step chain over
  completed antecedents goes from 480 bytes to 0. A `ValueTask` may be awaited
  only once; `AsTask()` and `AsValueTask()` bridge the two. (#32, #42)
- **`MatchAsync` on both async APIs.** A pipeline previously had to be awaited
  into a local and matched in synchronous code. (#43)
- **`ToHttpResult` and `ToActionResult` overloads taking an `HttpContext`,** which
  resolve the configuration registered by `AddVerdictProblemDetails`. The
  interfaces were registered in 2.7.0 and nothing ever resolved them. (#26)
- **`MultiResult.ErrorsDisposed` and `ErrorCollection.Detach`.** (#28)
- **`tests/Verdict.Allocation.Tests`**, covering every package: 41 operations
  that must allocate nothing, 15 with a named byte budget, and sustained
  concurrent use asserting no collection at any generation. (#25, #52)
- **`tests/Verdict.Aot.Smoke`**, a `PublishAot` console app that CI publishes and
  runs on every push. (#29)
- **`tests/Verdict.NetStandard.Tests`**, which loads the netstandard2.0 assets
  that shipped untested for eight releases. (#30)
- **`tests/Verdict.Docs.Tests`**, which checks the claims the documentation makes.
  (#54)
- **Secure-defaults and untrusted-payload tests**, asserting what a
  default-configured pipeline does rather than what it can be configured to do.
  (#53)
- **Eight package guides** under `docs/packages/`, which the docs index had
  linked to since before they existed, and `docs/design-decisions.md`. (#31, #57)

### Fixed

- **`LogError` and the custom-level `Log` dropped `Error.Exception`,** so the
  stack trace never reached the sink. `ResultLogger` always passed it. (#20)
- **`MultiResult.ToString()` threw after `DisposeErrors()`.** (#21)
- **`ErrorCollection` pooled where pooling lost.** It now pools only between 8 and
  1024 errors: below that the disposal contract cost more than the pool saved,
  above it `ArrayPool.Shared` retained a large object heap array for the life of
  the process. Forgetting to dispose went from 496 bytes an operation to 96.
  Nothing is dropped at any size. (#44, #48)
- **A publish could ship any branch to NuGet.** The tag check was guarded by the
  ref being a tag while the push was guarded only by `dry_run`, so a manual
  dispatch with `dry_run=false` skipped it. (#41)
- **`ProblemDetailsFactory.CreateFromMultiResult` threw on a released
  collection,** from inside the code building the error response. (#49)
- **The benchmark runner discarded its arguments,** so the scheduled workflow's
  `--filter` and `--exporters` did nothing and the JSON benchmarks never ran.
  (#22)

### Changed

- **CI runs the suite on .NET 10 as well as .NET 8**, installing only the matrix
  SDK so the net8.0 assets genuinely roll forward rather than silently re-running
  on 8.0. (#38)
- **Coverage has a floor.** The nine Cobertura reports are merged first, because
  each lists every package and a naive sum lands nowhere near the truth. Measured
  76%, not the 98.4% the badge claimed. (#40)
- **Dependency advisories fail the build**, at any severity, including transitive
  packages. (#51)
- **The examples project is in the solution**, so CI compiles it. (#39)
- **All eight packages carry an icon.** (#56)
- **Benchmarks measure ErrorOr and CSharpFunctionalExtensions**, not only
  FluentResults. All four allocate nothing on the success path, so that is table
  stakes rather than a differentiator. (#58)
- **README rewritten**, 546 lines to 252. It recommended
  `FromException(ex, sanitize: true)` as the way to avoid leaking details, which
  this release had to fix; it claimed 525 tests against a suite of 601; and it
  omitted the sustained-load result, which is the best evidence the project has.
  (#23, #55, #59)
- **SECURITY.md** lists the supported versions and states the trust boundary.
  (#50)

## [2.8.0] - 2026-08-18

### Fixed

- **`ErrorCollection.Create` leaked its pooled buffer when enumeration threw.**
  The `ICollection<Error>` fast path rented from `ArrayPool<Error>.Shared` with
  no `try`/`catch`, so a throwing enumerator lost the buffer until GC collected
  it, defeating the pooling the type exists for. It now returns the buffer on
  the exception path, and uses the number of items actually yielded rather than
  `Count`, so a collection whose `Count` overstates its enumeration no longer
  exposes stale pooled slots. Thanks to @snowyukitty. (#24, #34)
- **`ProblemDetailsFactory`'s process-wide defaults leaked between tests.** Four
  test classes shared that static while xUnit ran them in parallel, so a class
  mutating it could run alongside the class reading it. The suite was
  order-dependent rather than flaky: it passed on an incremental build and
  failed after a clean one. (#33, #36)

### Added

- **`net8.0` alongside `netstandard2.0` on every package.** Purely additive:
  existing consumers resolve exactly as before, and .NET 8 consumers now get a
  target the trimming and AOT analyzers can actually see, so a trim warning
  surfaces at build time rather than as a missing method at run time.
- **The zero-allocation promise is now enforced by a test, not asserted in the
  README.** `AllocationTests` measures allocations directly on the success path.
  Until now a contributor could break the reason to use this library without
  breaking a single behavioural test: add a field that boxes, capture a variable
  in a lambda, return an interface instead of the struct, and everything still
  went green while the benchmark quietly regressed.
- **The public API surface of all eight packages is pinned.** Any change to a
  signature, an accessibility, a base type or a default parameter now fails a
  test with a readable diff, and the failure message states the versioning rule
  it implies.

### Changed

- No public API was added, removed or altered in this release; the approval
  snapshots are unchanged from 2.7.0.

## [2.7.0] - 2026-08-07

### Fixed

- **`AddVerdictProblemDetails` registered nothing in the container.** It took an
  `IServiceCollection`, assigned a process-wide static, and returned the
  collection untouched. Two applications hosted in one process therefore shared
  a single configuration and the last registration won. Options and services are
  now registered properly and scoped to the container.

### Added

- `IVerdictProblemDetailsFactory` and `IErrorStatusCodeMapper`, both registered
  by `AddVerdictProblemDetails`, so ProblemDetails generation and status code
  mapping read the options of the container they are resolved from.
- `VerdictProblemDetailsOptions.StatusCodeMappings` for per-container error code
  to status code mappings, checked before the shared defaults. Prefer this to
  the process-wide `ErrorStatusCodeMapper.RegisterMapping`.
- `ProblemDetailsFactory.ResetDefaultOptions()` so tests can restore the
  process-wide default instead of leaking configuration into later tests.

### Notes

The static path is unchanged and still supported. `ToActionResult` and
`ToHttpResult` are extension methods with no access to DI, so they continue to
read the process-wide defaults, which `AddVerdictProblemDetails` still assigns.
For a single application both paths agree.

## [2.6.0] - 2026-08-07

### Added

- **Combinators for `MultiResult`.** Accumulating validation errors previously
  ended the chain: `MultiResult<T>` had no `Map`, `Bind` or `Match`, so the
  moment you used `EnsureAll` you dropped off the composable API. Adds `Map`,
  `Bind`, `Match`, `OnSuccess`, `OnFailure` and the async forms `MapAsync`,
  `BindAsync` and `OnSuccessAsync`, for both the generic and non-generic types.
  A failure carries its `ErrorCollection` through unchanged rather than copying
  it, so pooled buffers are neither duplicated nor orphaned, and the failure
  branch of `Match` and `OnFailure` receives that collection by value so nothing
  allocates.

### Changed

- Publishing now uses **NuGet Trusted Publishing**. The workflow exchanges a
  short-lived GitHub OIDC token for a NuGet key valid for one hour, so no
  long-lived API key is stored in the repository.


### Fixed

- **`WithTimeout` did not time out when given a `CancellationToken`.** The
  overload created a linked `CancellationTokenSource` and called `CancelAfter`,
  but never passed the token to anything. The awaited task was created by the
  caller and had no knowledge of it, so the timeout had no effect and the call
  waited for the operation to finish. All four overloads now share one race
  implementation.
- **Caller cancellation was reported as a timeout.** Cancelling the supplied
  token produced a timeout `Error` instead of an `OperationCanceledException`,
  so callers could not tell "I cancelled this" from "the service was slow".
- **A timed-out operation could surface as an unobserved task exception.** The
  abandoned task is now observed.
- `WithTimeout` validates its arguments: `ArgumentNullException` for a null
  task, `ArgumentOutOfRangeException` for a negative timeout.

### Added

- `WithTimeout(this Task<Result>, TimeSpan, Error, CancellationToken)`, the
  non-generic overload that was missing.

## [2.5.0] - 2026-08-07

### BREAKING CHANGE

**`ErrorCollection` accessors now throw `ObjectDisposedException` after `Dispose()`.**

Previously they kept working and returned whatever the pooled buffer happened to
contain, which in a server is another caller's data. Code that read after
disposal was already returning wrong values silently; it now fails loudly.

Affected members: `Count`, `HasErrors`, `AsSpan()`, `this[int]`, `First()`,
`ToArray()`.

`ErrorCollection` is a **struct**, so disposing any copy invalidates every copy.
Passing one to a method that disposes it leaves the caller's copy disposed too.

```csharp
// Before: read stale or foreign data, no error.
// Now:    throws ObjectDisposedException.
var errors = ErrorCollection.Create(list);
errors.Dispose();
var first = errors[0];

// Fix: finish reading before disposing.
using (var errors = ErrorCollection.Create(list))
{
    var first = errors[0];
}

// Or copy out what you need first.
Error[] snapshot;
using (var errors = ErrorCollection.Create(list))
{
    snapshot = errors.ToArray();
}
```

Check with `errors.IsDisposed` if ownership is unclear. Collections created by
`Create(Error)` or `Create(params Error[])` never use the pool and are
unaffected.

### Fixed (Critical)

- **`ErrorCollection` could read another caller's data after disposal.** `Dispose()`
  returns the buffer to `ArrayPool.Shared` but the struct kept the reference, so
  `Count`, the indexer, `AsSpan()`, `First()` and `ToArray()` continued to work and
  returned whatever the next renter wrote. Across requests that is one caller
  reading another's errors. All accessors now throw `ObjectDisposedException`.
  Because `ErrorCollection` is a struct, disposing any copy invalidates them all,
  which is now enforced rather than silent.

### Fixed

- **`Result<T>` and `Result` allocated on every equality comparison.** Neither
  overrode `Equals` or `GetHashCode`, so both fell through to reflection-based
  `ValueType.Equals`: 320 bytes per call, in a library whose guarantee is zero
  allocation. Any `HashSet`, dictionary key, `Contains` or `Distinct` paid it.
  Both now implement `IEquatable<>` with `==`, `!=` and `GetHashCode`. Measured
  320,000 bytes to 0 over 1,000 comparisons.
- **Corrected the thread-safety claim.** The package description, README and XML
  docs described `Result<T>` as thread-safe. It is immutable and safe to read
  concurrently, but it is 32-48 bytes, so concurrently reassigning a shared field
  can tear. A test observed 256,854 torn reads in 1.5 seconds. The docs now state
  the actual guarantee.
- **Corrected the GC pressure figure.** The README claimed 25 GB/sec saved at
  100k req/sec. From its own stated FluentResults allocation rate the correct
  figure is 18-38 MB/sec, roughly 1000x smaller.
- **Malformed XML doc comments** in `ResultExtensions` never compiled, because
  documentation generation had never been enabled.

### Added

- **Trimming and Native AOT support.** All packages except `Verdict.Json` are
  `IsTrimmable` and `IsAotCompatible` and publish clean under `PublishAot`.
  `ResultJsonConverter<T>` now resolves `JsonTypeInfo` from the caller's options
  rather than calling reflection-based `JsonSerializer`, and
  `AddVerdictConverter<T>()` / `AddVerdictResultConverter()` give an AOT-safe
  registration path. The convenience factory is annotated `[RequiresDynamicCode]`.
  Verified with a real `PublishAot` binary.
- **XML documentation now ships.** `GenerateDocumentationFile` was not set on any
  project, so every `///` comment was discarded at pack time and consumers got no
  IntelliSense.
- **SourceLink, symbol packages and deterministic builds** via a new
  `Directory.Build.props`, which is also the single source of the version.
- `ErrorCollection.IsDisposed`.

### Changed

- **The publish workflow now runs the tests** before packing. It previously went
  restore, build, pack, push.
- **The release version comes from `Directory.Build.props`**, not a hand-typed
  workflow input, and the tag is checked against it. Previously the published
  version existed nowhere in source: `csproj` said 2.3.0 while nuget.org had 2.4.0.
- `TreatWarningsAsErrors` on all shipping projects.

### Known limitations

- `Error.Exception` is a public property, so System.Text.Json's source generator
  descends into `System.Exception` and emits two `IL2026` warnings for
  `TargetSite`. Harmless at runtime; `ErrorJsonConverter` never serializes it.
  Replacing the property with a method is planned for the next major version.

## [2.4.0] - 2026-03-02

### Fixed (Critical)

- **Fixed double-dispose pool corruption in `ErrorCollection`**: Introduced `RentalTracker` reference-type wrapper with `Interlocked.Exchange` to ensure idempotent disposal. All struct copies now share the same tracker, preventing the same array from being returned to `ArrayPool` twice.
- **Fixed `clearArray: false` retaining `Exception` references in pool**: Changed all `ArrayPool.Return` calls to use `clearArray: true`, preventing GC from retaining exception objects referenced through un-cleared pooled arrays.
- **Fixed RFC 7807 compliance**: Added `contentType: "application/problem+json"` to all ProblemDetails responses in `ResultExtensions`, making responses compliant with the RFC 7807 specification.
- **Fixed `CreatedResult`/`AcceptedResult` empty `Location` header**: 201/202 responses without a location URI now return `ObjectResult` instead of `CreatedResult(string.Empty, ...)`. Added `locationUri` parameter to `ToActionResult` for proper REST semantics when a location is available.

### Performance

- **Fixed `ImmutableDictionary.Create<K,V>().Add()` double allocation**: Replaced with `ImmutableDictionary<string, object>.Empty.Add()` in `RichResult`, `RichResultNonGeneric`, and `SuccessInfo` to avoid allocating an intermediate empty dictionary on first metadata entry.
- **Fixed `WithCustomError` O(N) intermediate allocations**: Replaced per-entry `WithErrorMetadata` loop with `ImmutableDictionary.CreateBuilder()` in `IErrorMetadata.cs`, reducing N intermediate dictionary allocations to 1. Added internal `WithErrorMetadataBulk` method to `RichResult<T>` and `RichResult`.
- **Fixed `ResultLogger` to use `LoggerMessage.Define`**: Replaced all direct `logger.LogDebug`/`LogInformation`/`LogError` calls with pre-compiled `LoggerMessage.Define` delegates, eliminating `object[]` allocations on every log call.
- **Removed LINQ from zero-allocation core**: Replaced `code.All(...)` in `Error.IsValidErrorCode` with a manual `foreach` loop. Removed unused `using System.Linq` from `Error.cs` and `ValidationExtensions.cs`.

### Improved

- **Added missing ProblemDetails entries**: Added `GetProblemType`/`GetTitle` entries for HTTP 402, 429, 502, 503, 504 status codes. Updated all RFC URIs from obsolete RFC 7231 to current RFC 9110.

### Testing

- All 525 tests passing (2 new tests added for `locationUri` feature)
- New tests for `ToActionResult` with `locationUri` parameter (Created and Accepted responses)

---

## [2.3.0] - 2026-01-18

### Security Hardening

- **[Obsolete] Warnings**: Added deprecation warnings to `FromException` methods that expose raw exception messages
- **Secure-by-Default**: `TryExtensions` now sanitizes exception messages by default
- **Null Logger Handling**: `ResultLogger` now throws `ArgumentNullException` instead of silently handling null loggers

### Performance (Zero-Allocation Improvements)

- Replaced LINQ allocations in `CombineExtensions.Merge` with manual loops
- Fixed double allocation in `ErrorCollection.Create(IEnumerable)`
- Added fast paths for arrays and `ICollection<Error>` in `ErrorCollection`
- Changed `SuccessInfo` to use `ImmutableDictionary` instead of `Dictionary` copy
- Pre-allocate array in `ProblemDetailsFactory.CreateFromMultiResult`

### Fixed

- Fixed race condition in `ProblemDetailsFactory` using `Interlocked.Exchange`
- Fixed null handling in `ErrorCollection.Create` to throw `ArgumentNullException`
- Removed dead code `ResultConfiguration.cs` from Verdict.Rich

### Testing

- All 523 tests passing (176 new tests added since 2.1.0)
- **Production Readiness Tests** for Extensions, Json, AspNetCore packages
  - Disposal patterns and thread-safety validation
  - Copy semantics and concurrent access patterns
  - Large payload handling and edge cases
- **Security Edge Case Tests** for Async, Fluent, and Json extensions
  - Cancellation token edge cases
  - Timeout boundary tests
  - Malformed JSON handling
  - Deep chaining and pattern matching tests

### Examples

- Added `ZeroAllocationExample` demonstrating stack-based struct usage
- Added `ThreadSafeUsageExample` for concurrent access patterns
- Added `ProperDisposalExample` for ArrayPool management
- Added `ProductionTryPatternExample` for sanitized exception handling

---

## [2.2.0] - 2026-01-15

### Added

#### New Package: Verdict.Json

A new package for System.Text.Json serialization of Result types:

- `ResultJsonConverter<T>` - Serializes `Result<T>` with `isSuccess`, `value`, and `error` properties
- `ResultNonGenericJsonConverter` - Serializes non-generic `Result`
- `ErrorJsonConverter` - Serializes `Error` with `code`, `message` properties
- `ResultJsonConverterFactory` - Auto-detects and applies correct converter
- `VerdictJsonExtensions.AddVerdictConverters()` - Easy `JsonSerializerOptions` configuration

```csharp
var options = new JsonSerializerOptions().AddVerdictConverters();
var json = JsonSerializer.Serialize(Result<int>.Success(42), options);
// {"isSuccess":true,"value":42}
```

#### Security Features (Verdict Core)

- **Error Sanitization**: `Error.FromException(exception, sanitize: true)` - Prevents leaking sensitive exception details
- **Error Code Validation**: `Error.CreateValidated(code, message)` - Validates codes contain only alphanumeric characters and underscores
- **Validation Helper**: `Error.ValidateErrorCode(code)` - Static validation method
- **Code Checking**: `Error.IsValidErrorCode(code)` - Returns bool without throwing

#### ASP.NET Core Enhancements

- **VerdictProblemDetailsOptions** - Configure ProblemDetails generation:
  - `IncludeExceptionDetails` (default: false) - Hide exception types in production
  - `IncludeStackTrace` (default: false) - Hide stack traces in production
  - `IncludeErrorCode` (default: true) - Include error codes in extensions
  - `IncludeErrorMessage` (default: true) - Show/hide error messages
  - `GenericServerErrorMessage` - Customizable fallback message
- **ServiceCollectionExtensions** - DI registration helpers
- **Thread-safe ErrorStatusCodeMapper** - Safe for concurrent use

#### Async Extensions (Verdict.Async)

CancellationToken and timeout support for async chains:

- `MapAsync<T, K>(Func<T, CancellationToken, Task<K>>, CancellationToken)`
- `BindAsync<T, K>(Func<T, CancellationToken, Task<Result<K>>>, CancellationToken)`
- `TapAsync(Func<T, CancellationToken, Task>, CancellationToken)`
- `EnsureAsync(Func<T, CancellationToken, Task<bool>>, Error, CancellationToken)`
- `WithTimeout(TimeSpan)` - Timeout wrapper for async operations

```csharp
await GetUserAsync(id)
    .BindAsync((user, ct) => ValidateAsync(user, ct), cancellationToken)
    .MapAsync((user, ct) => TransformAsync(user, ct), cancellationToken);
```

#### Validation Extensions (Verdict.Extensions)

- `Ensure<T>(Func<T, bool>, Func<T, Error>)` - Dynamic error factory for context-aware error messages
- `Ensure<T>` overload for `MultiResult<T>` with error factory

```csharp
result.Ensure(
    user => user.Age >= 18,
    user => new Error("UNDERAGE", $"User {user.Name} must be 18+, but is {user.Age}"));
```

### Improved

- JSON deserialization validation - Throws `JsonException` when `isSuccess=false` but error is missing
- Better LINQ readability with `.All()` positive logic pattern
- Enhanced XML documentation throughout

### Fixed

- JSON deserialization now validates required fields to prevent invalid Result states
- Removed unused `hasValueProperty` variable in `ResultJsonConverter`

### Testing

- All 347 tests passing (65 new tests added)
- Comprehensive JSON serialization/deserialization tests
- Error sanitization and validation tests
- CancellationToken and timeout tests
- Dynamic error factory tests

### Benchmarks

- Added competitive JSON serialization benchmarks
- Verdict.Json performance compared against manual serialization

---

## [2.1.0] - 2026-01-09

### BREAKING CHANGES

#### Critical Bug Fixes for Production Safety

This release fixes **3 critical issues** that could cause memory corruption, resource leaks, and silent bugs. While these are breaking changes, they only affect a small portion of the API and significantly improve reliability.

**1. Fixed ArrayPool Memory Corruption (Critical - CVE Pending)**

`ErrorCollection.Dispose()` previously used `clearArray: true`, which could cause data corruption when structs were copied.

**Changed:**
- `ErrorCollection.Dispose()` now uses `clearArray: false` to prevent corruption
- No code changes required (internal change)
- Fixes potential data leaks in concurrent scenarios

**2. Removed IDisposable Anti-Pattern from MultiResult (Critical)**

`MultiResult<T>` and `MultiResult` implementing `IDisposable` was an anti-pattern that broke disposal semantics due to struct copy-by-value behavior.

**API Changes:**
- `MultiResult<T>` no longer implements `IDisposable`
- `MultiResult` no longer implements `IDisposable`
- `Dispose()` method renamed to `DisposeErrors()`

**Migration:**
```csharp
// BEFORE (v2.0)
using var result = MultiResult<int>.Failure(errors);
result.Dispose();

// AFTER (v2.1)
var result = MultiResult<int>.Failure(errors);
result.DisposeErrors();
```

**Impact:** Low - Most users don't dispose results. See [MIGRATION_v2.0_to_v2.1.md](docs/MIGRATION_v2.0_to_v2.1.md) for details.

**3. Added Validation for Default Struct State (Critical)**

`default(Result<T>)` created invalid states that led to silent bugs.

**Changed:**
- Accessing `Error` property on default-initialized `Result<T>` now throws `InvalidOperationException` with helpful message
- Prevents silent bugs from uninitialized results

**Exception Message:**
```
InvalidOperationException: Result is in invalid state (likely from default struct initialization).
Always use Result<T>.Success() or Result<T>.Failure() to create results.
```

**Impact:** Very Low - Rare pattern. Helps catch bugs early.

### Improved

- Better error messages for `ErrorCollection` index out of range exceptions
- Enhanced XML documentation for disposal semantics
- Added warnings about struct copy behavior

### Fixed

- **Security:** Memory corruption vulnerability in `ErrorCollection` when structs are copied
- **Reliability:** Resource leaks from broken IDisposable pattern on structs
- **Correctness:** Silent bugs from default struct initialization

### Documentation

- Added comprehensive migration guide: [docs/MIGRATION_v2.0_to_v2.1.md](docs/MIGRATION_v2.0_to_v2.1.md)
- Added code review and improvement plan: [docs/code_review_and_improvement_plan.md](docs/code_review_and_improvement_plan.md)
- Updated benchmark results: [docs/benchmark_results_v2.0.md](docs/benchmark_results_v2.0.md)

### Testing

- All 282 tests passing
- Added validation for disposal edge cases
- Verified zero allocation promise maintained

---

## [2.0.0] - 2026-01-02

### BREAKING CHANGES

#### Verdict.Rich Package Redesign

The Rich package has been completely redesigned to fix a critical memory leak vulnerability (CVSS 7.5). Metadata is now embedded directly in the `RichResult<T>` struct instead of using external storage.

**API Changes:**

1. **Return Type Changes**
   - `Result<T>.WithSuccess(string)` now returns `RichResult<T>` (was `Result<T>`)
   - `Result<T>.WithErrorMetadata(string, object)` now returns `RichResult<T>` (was `Result<T>`)
   - `Result.WithSuccess(string)` now returns `RichResult` (was `Result`)
   - `Result.WithErrorMetadata(string, object)` now returns `RichResult` (was `Result`)

2. **Method to Property Changes**
   - `result.GetSuccesses()` → `result.Successes` (now a property)
   - `result.GetErrorMetadata()` → `result.ErrorMetadata` (now a property)

**Migration Guide:**

```csharp
// BEFORE (v1.0):
Result<int> result = Result<int>.Success(42)
    .WithSuccess("Step 1")
    .WithSuccess("Step 2");
var successes = result.GetSuccesses();
var metadata = result.GetErrorMetadata();

// AFTER (v2.0):
RichResult<int> result = Result<int>.Success(42)
    .WithSuccess("Step 1")
    .WithSuccess("Step 2");
var successes = result.Successes;      // Property instead of method
var metadata = result.ErrorMetadata;   // Property instead of method
```

**Implicit Conversions:**

The new design includes implicit conversions for easier migration:

```csharp
// Auto-converts Result<T> to RichResult<T>
RichResult<int> rich = Result<int>.Success(42);

// Auto-converts back (metadata is lost)
Result<int> plain = rich;
```

### Fixed

- **CRITICAL**: Fixed memory leak in Verdict.Rich metadata storage (CWE-401)
  - Replaced `ConcurrentDictionary` with embedded `ImmutableList` and `ImmutableDictionary`
  - Eliminated unbounded memory growth in long-running applications
  - Fixed metadata cross-contamination between Result instances with equal values
  - CVSS v3.1 Score: 7.5 (High) → 0.0 (None)

### Added

- New `RichResult<T>` struct with embedded metadata
- New `RichResult` (non-generic) struct with embedded metadata
- Implicit conversions between `Result<T>` and `RichResult<T>`
- `System.Collections.Immutable` dependency for efficient metadata operations

### Changed

- Verdict.Rich now uses embedded metadata architecture
- All 282 tests pass (previously 278/282 due to vulnerability)
- Improved thread safety through immutable design

### Removed

- Deleted `ResultMetadata.cs` (external storage no longer needed)
- Removed `GetSuccesses()` method (replaced with `Successes` property)
- Removed `GetErrorMetadata()` method (replaced with `ErrorMetadata` property)

---

## [1.0.0] - 2025-12-26

### Added
- **Core Library**: High-performance, zero-allocation `Result<T>` and `Result` implementations.
- **Extensions Package**: Functional composition helpers (`Map`, `Bind`, `Tap`, `Combine`, `Validation`).
- **Logging Package**: High-performance logging extensions using `LoggerMessage.Define`.
- **AspNetCore Package**: Minimal API and MVC integration with RFC 7807 Problem Details support.
- **Async Package**: First-class `Task<Result<T>>` support for seamless async pipelines.
- **Rich Package**: Externalized metadata support for adding success messages and error context without bloating the Result struct.
- **Fluent Package**: Functional pattern matching and chainable API enhancements.
- **Comprehensive Docs**: `SECURITY.md`, `README.md` enhancements, and architectural decision guides.

### Performance
- Zero-allocation success path.
- Minimal overhead for failure path (no stack trace generation unless explicitly requested).
- Optimized for L1/L2 cache locality using small, stack-allocated structs.
- Outperforms popular alternatives like `FluentResults` in high-throughput scenarios.

### Fixed
- CS8618 warning in `Result.cs`.
- Minor bugs in Result deconstruction.

---
[2.4.0]: https://github.com/BaryoDev/Verdict/releases/tag/v2.4.0
[2.3.0]: https://github.com/BaryoDev/Verdict/releases/tag/v2.3.0
[2.2.0]: https://github.com/BaryoDev/Verdict/releases/tag/v2.2.0
[2.1.0]: https://github.com/BaryoDev/Verdict/releases/tag/v2.1.0
[2.0.0]: https://github.com/BaryoDev/Verdict/releases/tag/v2.0.0
[1.0.0]: https://github.com/BaryoDev/Verdict/releases/tag/v1.0.0
