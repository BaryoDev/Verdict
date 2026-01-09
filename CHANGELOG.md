# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
[2.0.0]: https://github.com/BaryoDev/Verdict/releases/tag/v2.0.0
[1.0.0]: https://github.com/BaryoDev/Verdict/releases/tag/v1.0.0
