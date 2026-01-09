# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Verdict is a high-performance Result pattern library for .NET that provides zero-allocation error handling. It's designed as a drop-in replacement for FluentResults with 189x better performance and zero GC pressure.

**Core Philosophy:** Start with zero-allocation core. Scale to enterprise features through opt-in packages. Never compromise on speed.

## Build, Test, and Run Commands

### Building the Solution
```bash
# Build entire solution
dotnet build Verdict.sln

# Build in Release mode
dotnet build Verdict.sln -c Release

# Build specific project
dotnet build src/Verdict/Verdict.csproj
```

### Running Tests
```bash
# Run all tests
dotnet test Verdict.sln

# Run tests for specific project
dotnet test tests/Verdict.Tests/Verdict.Tests.csproj

# Run tests with verbosity
dotnet test Verdict.sln -v detailed

# Run a single test
dotnet test --filter "FullyQualifiedName~ResultSuccessTests"
```

### Running Benchmarks
```bash
# Run all benchmarks
dotnet run -c Release --project benchmarks/Verdict.Benchmarks

# Important: Always use Release configuration for accurate performance measurements
```

### Package Management
```bash
# Restore dependencies
dotnet restore Verdict.sln

# Pack NuGet packages (creates in nupkgs/)
dotnet pack src/Verdict/Verdict.csproj -c Release -o nupkgs/
```

## Architecture

### Package Structure

Verdict follows a modular architecture with clear separation of concerns:

1. **Verdict** (Core) - Zero dependencies, zero allocation
   - `Result<T>` - Generic result with value or error
   - `Result` - Non-generic result (void operations)
   - `Error` - Lightweight error representation (`readonly record struct`)
   - All types are `readonly struct` for stack allocation and immutability

2. **Verdict.Extensions** - Multi-error support
   - `MultiResult<T>` - Result with multiple errors
   - `ErrorCollection` - Pooled error collection (uses `ArrayPool<T>`)
   - `ValidationExtensions` - Validation helpers
   - `CombineExtensions` - Combine multiple results
   - Dependency: `System.Memory` (for `ArrayPool<T>`)

3. **Verdict.Fluent** - Functional extensions
   - `Match<T, TOut>` - Pattern matching
   - `Map<T, K>` - Transform success value
   - `OnSuccess` / `OnFailure` - Side effects
   - Zero dependencies

4. **Verdict.Async** - Async/await support
   - `MapAsync`, `BindAsync`, `TapAsync` - Async transformations
   - `EnsureAsync` - Async validation
   - Zero dependencies, async extension methods

5. **Verdict.Rich** - Success/error metadata (v2.0 redesign)
   - `RichResult<T>` - Result with embedded metadata
   - `RichResultNonGeneric` - Non-generic rich result
   - **CRITICAL:** Metadata is now embedded in the struct (uses `ImmutableList`, `ImmutableDictionary`)
   - **BREAKING CHANGE (v2.0):** `WithSuccess()` and `WithErrorMetadata()` now return `RichResult<T>` instead of `Result<T>`
   - Includes implicit conversions between `Result<T>` and `RichResult<T>`

6. **Verdict.Logging** - Logging integration
   - Integrates with `Microsoft.Extensions.Logging`
   - Automatic logging of success/failure

7. **Verdict.AspNetCore** - Web integration
   - Automatic conversion to `IActionResult`
   - HTTP status code mapping

### Key Design Patterns

#### 1. Struct-Based Design
All core types are `readonly struct` for:
- Stack allocation (when possible)
- Zero GC pressure on success path
- Thread-safe immutability
- Copy semantics (no reference aliasing)

#### 2. Implicit Conversions
Makes API ergonomic without allocation overhead:
```csharp
Result<int> GetValue() => 42;  // T -> Result<T>
Result<int> GetError() => new Error("NOT_FOUND", "..."); // Error -> Result<T>
```

#### 3. Embedded Metadata (Rich v2.0)
**Changed in v2.0:** Metadata is now stored directly in the struct using `ImmutableList` and `ImmutableDictionary` instead of external thread-local storage. This eliminates memory leaks but changes the return type of metadata methods:

```csharp
// Returns RichResult<T>, not Result<T>
RichResult<int> result = Result<int>.Success(42)
    .WithSuccess("Step completed");

// Access metadata via properties, not methods
var successes = result.Successes;       // Property (was GetSuccesses() method)
var metadata = result.ErrorMetadata;    // Property (was GetErrorMetadata() method)
```

#### 4. Pool-Based Collections
`ErrorCollection` uses `ArrayPool<T>` for multi-error scenarios to minimize allocations. **Important:** `MultiResult<T>` implements `IDisposable` - always dispose when done to return arrays to pool.

## Critical Implementation Details

### 1. Zero-Allocation Success Path
The core `Result<T>` has **zero allocation** on success path:
- Struct lives on stack (when not captured)
- No Error object created
- No exception thrown
- No heap allocations

### 2. Nullable Value Handling
The `_value` field is marked `default!` in failure constructor because:
- `IsSuccess = false` guarantees `Value` property will never be accessed
- `Value` getter throws `InvalidOperationException` when `IsFailure == true`
- This is safe and avoids unnecessary null-checking overhead

### 3. Thread Safety
All types are immutable after construction:
- `readonly struct` prevents mutation
- No setters on properties
- `RichResult` uses immutable collections (`ImmutableList`, `ImmutableDictionary`)

### 4. Performance Characteristics
- Success path: **0 bytes allocated**
- Failure path: **0 bytes allocated** (core), ~96 bytes (with metadata)
- Multi-error: ~200 bytes (pooled)
- **Always dispose `MultiResult<T>`** to return buffers to pool

## Development Guidelines

### When Adding New Features

1. **Maintain Zero Allocation** - Core package must remain zero-allocation on success path
2. **Use Struct Types** - Prefer `readonly struct` over `class` for new result types
3. **Opt-In Complexity** - New features should be in extension packages, not core
4. **Test Performance** - Add benchmark tests for any performance-sensitive changes
5. **Preserve Immutability** - Never add mutable state to result types

### Testing Patterns

- Tests use xUnit framework
- Test structure: `[PackageName].Tests/[FeatureName]Tests.cs`
- Edge cases and security tests are separate files
- Always test both success and failure paths
- Include tests for implicit conversions

### Performance Testing

Always benchmark changes that could affect performance:
```bash
dotnet run -c Release --project benchmarks/Verdict.Benchmarks
```

Compare against FluentResults and Exceptions to ensure performance claims remain valid.

## Common Patterns

### Pattern: Converting Between Result Types

```csharp
// Result<T> -> RichResult<T> (implicit)
RichResult<int> rich = Result<int>.Success(42);

// RichResult<T> -> Result<T> (implicit, loses metadata)
Result<int> plain = rich;

// MultiResult<T> -> Result<T>
Result<int> single = multiResult.ToSingleResult();
```

### Pattern: Validation with Multiple Errors

```csharp
// Use MultiResult<T> from Verdict.Extensions
var errors = new List<Error>();
if (age < 0) errors.Add(new Error("INVALID_AGE", "Age cannot be negative"));
if (string.IsNullOrEmpty(name)) errors.Add(new Error("INVALID_NAME", "Name is required"));

return errors.Any()
    ? MultiResult<User>.Failure(errors.ToArray())
    : MultiResult<User>.Success(user);
```

### Pattern: Async Operations with Railway-Oriented Programming

```csharp
// Chain async operations (Verdict.Async)
return await ValidateUserAsync(request)
    .BindAsync(CreateUserAsync)
    .TapAsync(user => SendWelcomeEmailAsync(user))
    .MapAsync(user => new UserDto(user));
```

## Version Notes

**Current Version:** 2.0.0 (Breaking changes in Verdict.Rich)

- See CHANGELOG.md for migration guide from v1.0 to v2.0
- Breaking change: `WithSuccess()` and `WithErrorMetadata()` now return `RichResult<T>`
- Memory leak fixed in v2.0 by embedding metadata in struct
