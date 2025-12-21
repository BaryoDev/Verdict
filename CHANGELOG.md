# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
[1.0.0]: https://github.com/BaryoDev/Verdict/releases/tag/v1.0.0
