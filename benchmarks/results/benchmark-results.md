# Upshot Competitive Benchmarks

This report contains the verified benchmark results comparing Upshot against native C# exceptions, FluentResults, and LanguageExt.

## Hardware Environment
- **OS:** macOS 15.2 (25C56) [Darwin 25.2.0]
- **Processor:** Apple M1 (8 cores)
- **Memory:** 16 GB
- **SDK:** .NET 8.0.107
- **Runtime:** .NET 8.0.7 (8.0.724.31311), Arm64 RyuJIT AdvSIMD

## Summary of Results

### 1. Success Path (Happy Path)
*1,000 iterations of a successful division operation.*

| Library       | Mean       | Allocated | vs Upshot            |
| ------------- | ---------- | --------- | -------------------- |
| **Upshot**    | **335 ns** | **0 B**   | **1.00x (baseline)** |
| Exceptions    | 336 ns     | 0 B       | 1.00x                |
| LanguageExt   | 1,326 ns   | 0 B       | 3.96x slower         |
| FluentResults | 63,303 ns  | 176,000 B | **189x slower** ⚠️    |

### 2. Failure Path (Error Handling)
*1,000 iterations of a failed division operation.*

| Library       | Mean          | Allocated | vs Upshot            |
| ------------- | ------------- | --------- | -------------------- |
| **Upshot**    | **626 ns**    | **0 B**   | **1.00x (baseline)** |
| LanguageExt   | 2,160 ns      | 96 B      | 3.45x slower         |
| FluentResults | 91,343 ns     | 368,000 B | **146x slower** ⚠️    |
| Exceptions    | 16,836,328 ns | 344,023 B | **26,890x slower** ⚠️ |

### 3. Mixed Workload (90% success, 10% failure)
*Realistic scenario with 900 successes and 100 failures.*

| Library       | Mean         | Allocated | vs Upshot            |
| ------------- | ------------ | --------- | -------------------- |
| **Upshot**    | **1,276 ns** | **0 B**   | **1.00x (baseline)** |
| LanguageExt   | 1,975 ns     | 0 B       | 1.55x slower         |
| FluentResults | 92,422 ns    | 245,600 B | **72x slower** ⚠️     |
| Exceptions    | 1,626,148 ns | 22,401 B  | **1,274x slower** ⚠️  |

---

## Analysis & ROI

### Memory Efficiency
Upshot delivers **zero-allocation** performance on all paths. In high-throughput applications, this eliminates significant GC pressure and improves overall system stability. 

**Example:** In an API processing 100,000 requests per second with a 10% error rate, FluentResults would allocate approximately **24 GB per second** of temporary objects. Upshot reduces this to **zero**.

### CPU Efficiency
By avoiding stack trace generation (Exceptions) and heap allocations (FluentResults), Upshot allows the CPU to focus on business logic rather than infrastructure overhead.

---
*Report Generated: December 2025 using BenchmarkDotNet v0.13.12*
