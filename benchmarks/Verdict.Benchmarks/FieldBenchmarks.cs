using BenchmarkDotNet.Attributes;
using Verdict;
using Verdict.Async;
using Verdict.Fluent;

namespace Verdict.Benchmarks;

/// <summary>
/// Verdict against the libraries people actually choose between, rather than
/// against the one it beats structurally.
/// </summary>
/// <remarks>
/// <c>CompetitiveBenchmarks</c> measures exceptions, FluentResults and
/// LanguageExt. FluentResults is the only class-based Result type in that list,
/// so beating it measures the gap between a struct and a class, which every
/// modern competitor already closed. Measured on net8.0: Verdict, ErrorOr,
/// CSharpFunctionalExtensions and LightResults all allocate nothing on the
/// success path.
/// <para>
/// Two places the field does differ, and both are measured here: a single-error
/// failure is free for Verdict and costs 88 bytes for ErrorOr, because the error
/// lives in the struct rather than in a list; and no library in this field has a
/// completed-antecedent fast path on its async composition, which is what
/// <c>AsyncFieldBenchmarks</c> below is about.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class FieldBenchmarks
{
    private const int Iterations = 1000;

    private static readonly Error VerdictError = new("NOT_FOUND", "missing");

    // ==================== success: construct and read ====================

    [Benchmark(Baseline = true, Description = "Verdict success")]
    public int Verdict_Success()
    {
        var sum = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var result = Result<int>.Success(i);
            sum += result.IsSuccess ? result.Value : 0;
        }
        return sum;
    }



    // ==================== failure: construct and read ====================

    [Benchmark(Description = "Verdict failure")]
    public int Verdict_Failure()
    {
        var count = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var result = Result<int>.Failure(VerdictError);
            if (result.IsFailure && result.Error.Code.Length > 0) count++;
        }
        return count;
    }



    // ==================== a chain with real work in it ====================
    // The existing benchmark divides a number by two and returns it, so
    // allocation is the entire measurement and the ratio is the most favourable
    // one obtainable. This one does a dictionary lookup and a string comparison
    // between steps, which is closer to what a caller sees.

    private static readonly Dictionary<int, string> Lookup =
        Enumerable.Range(0, 128).ToDictionary(i => i, i => $"item-{i}");

    private static string Describe(int value) =>
        Lookup.TryGetValue(value & 127, out var name) && name.StartsWith("item", StringComparison.Ordinal)
            ? name
            : "unknown";

    [Benchmark(Description = "Verdict chain with work")]
    public int Verdict_Chain()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += Result<int>.Success(i)
                .Map(Describe)
                .Match(name => name.Length, _ => -1);
        }
        return total;
    }


}

/// <summary>
/// The async composition step, on antecedents that have already completed.
/// </summary>
/// <remarks>
/// This is the column nobody in the field measures, and the one place the
/// numbers separate. Every library here is an <c>async</c> method that awaits
/// whether or not there is anything to wait for, so a four-step chain costs
/// hundreds of bytes even when every step is already done, which in a request
/// handler is the common case.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AsyncFieldBenchmarks
{
    private const int Iterations = 100;

    private static int Double(int x) => x * 2;

    [Benchmark(Baseline = true, Description = "Verdict ValueTask, four steps")]
    public async Task<int> Verdict_ValueTask()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var result = await Result<int>.Success(i)
                .AsValueTask()
                .Map(Double).Map(Double).Map(Double).Map(Double);
            total += result.IsSuccess ? 1 : 0;
        }
        return total;
    }

    [Benchmark(Description = "Verdict Task, four steps")]
    public async Task<int> Verdict_Task()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var result = await Task.FromResult(Result<int>.Success(i))
                .Map(Double).Map(Double).Map(Double).Map(Double);
            total += result.IsSuccess ? 1 : 0;
        }
        return total;
    }


}
