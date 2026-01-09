# Verdict Examples

This directory contains comprehensive examples demonstrating all features of the Verdict library.

## Running the Examples

```bash
cd examples
dotnet run
```

## What's Included

The `Examples.cs` file showcases **20 comprehensive use cases** covering:

### Core Package (5 examples)
1. **Basic Usage** - Simple success/failure scenarios
2. **Implicit Conversions** - Automatic type conversion
3. **Non-Generic Result** - For void operations
4. **Error Handling with Exceptions** - Convert exceptions to Results
5. **Value Or Fallback** - Provide default values

### Fluent Package (3 examples)
6. **Fluent API Chaining** - Transform results with Map
7. **Pattern Matching** - Match on success/failure
8. **Railway-Oriented Programming** - Chain operations that short-circuit on error

### Extensions Package (4 examples)
9. **Multi-Error Validation** - Return multiple validation errors
10. **Combine Operations** - Merge multiple results
11. **Try Pattern** - Automatically catch exceptions
12. **Error Collection Disposal** - Manage ArrayPool correctly

### Async Package (2 examples)
13. **Async Pipeline** - Chain async operations seamlessly
14. **Async Error Handling** - Handle errors in async chains

### Rich Package (3 examples)
15. **Rich Metadata** - Add success messages and error metadata
16. **Success Messages** - Track operation steps for auditing
17. **Error Metadata** - Add diagnostic information to errors

### Real-World Scenarios (3 examples)
18. **Form Validation** - E-commerce checkout with multiple errors
19. **API Call Chain** - Microservice communication with error propagation
20. **Domain-Driven Design** - Aggregate validation with business rules

## Output

The examples produce formatted console output showing:
- ✓ Successful operations
- ✗ Failed operations
- Step-by-step execution flow
- Error details and diagnostics

## Learning Path

**Beginners:** Start with examples 1-5 (Core Package)
**Intermediate:** Move to examples 6-14 (Fluent, Extensions, Async)
**Advanced:** Study examples 15-20 (Rich Package & Real-World Scenarios)

## Code Structure

Each example is self-contained and demonstrates:
- How to create Results
- How to handle success/failure
- Best practices for each package
- Real-world usage patterns

## Additional Resources

- [Main README](../README.md) - Library overview
- [CLAUDE.md](../CLAUDE.md) - Development guide
- [Architect's Guide](../docs/architects_decision_guide.md) - Decision-making guide
- [Migration Guide](../docs/MIGRATION_v2.0_to_v2.1.md) - Upgrade instructions

## Performance Note

All examples use the zero-allocation features of Verdict. Even with 20 examples running, the memory footprint remains minimal compared to exception-based or FluentResults-based implementations.
