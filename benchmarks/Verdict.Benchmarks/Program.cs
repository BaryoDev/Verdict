using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Verdict.Benchmarks;

/// <summary>
/// Competitive benchmarks comparing Verdict against:
/// 1. Exceptions (native C#)
/// 2. FluentResults (most popular Result library)
/// 3. LanguageExt (functional programming library)
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class CompetitiveBenchmarks
{
    private const int Iterations = 1000;

    // ==================== SUCCESS PATH ====================

    [Benchmark(Baseline = true, Description = "Native Exceptions (Success)")]
    public int Exception_Success()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            sum += DivideWithException(100, 2);
        }
        return sum;
    }

    [Benchmark(Description = "FluentResults (Success)")]
    public int FluentResults_Success()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithFluentResults(100, 2);
            sum += result.Value;
        }
        return sum;
    }

    [Benchmark(Description = "LanguageExt Either (Success)")]
    public int LanguageExt_Success()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithLanguageExt(100, 2);
            sum += result.Match(
                Right: value => value,
                Left: error => 0
            );
        }
        return sum;
    }

    [Benchmark(Description = "Verdict (Success)")]
    public int Verdict_Success()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithVerdict(100, 2);
            sum += result.Value;
        }
        return sum;
    }

    // ==================== FAILURE PATH ====================

    [Benchmark(Description = "Native Exceptions (Failure)")]
    public int Exception_Failure()
    {
        var errorCount = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                DivideWithException(100, 0);
            }
            catch (DivideByZeroException)
            {
                errorCount++;
            }
        }
        return errorCount;
    }

    [Benchmark(Description = "FluentResults (Failure)")]
    public int FluentResults_Failure()
    {
        var errorCount = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithFluentResults(100, 0);
            if (result.IsFailed)
            {
                errorCount++;
            }
        }
        return errorCount;
    }

    [Benchmark(Description = "LanguageExt Either (Failure)")]
    public int LanguageExt_Failure()
    {
        var errorCount = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithLanguageExt(100, 0);
            result.Match(
                Right: value => 0,
                Left: error => { errorCount++; return 0; }
            );
        }
        return errorCount;
    }

    [Benchmark(Description = "Verdict (Failure)")]
    public int Verdict_Failure()
    {
        var errorCount = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithVerdict(100, 0);
            if (result.IsFailure)
            {
                errorCount++;
            }
        }
        return errorCount;
    }

    // ==================== MIXED WORKLOAD (90% success, 10% failure) ====================

    [Benchmark(Description = "Native Exceptions (Mixed)")]
    public int Exception_Mixed()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                sum += DivideWithException(100, i % 10 == 0 ? 0 : 2);
            }
            catch (DivideByZeroException)
            {
                // Handle error
            }
        }
        return sum;
    }

    [Benchmark(Description = "FluentResults (Mixed)")]
    public int FluentResults_Mixed()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithFluentResults(100, i % 10 == 0 ? 0 : 2);
            if (result.IsSuccess)
            {
                sum += result.Value;
            }
        }
        return sum;
    }

    [Benchmark(Description = "LanguageExt Either (Mixed)")]
    public int LanguageExt_Mixed()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithLanguageExt(100, i % 10 == 0 ? 0 : 2);
            sum += result.Match(
                Right: value => value,
                Left: error => 0
            );
        }
        return sum;
    }

    [Benchmark(Description = "Verdict (Mixed)")]
    public int Verdict_Mixed()
    {
        var sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = DivideWithVerdict(100, i % 10 == 0 ? 0 : 2);
            if (result.IsSuccess)
            {
                sum += result.Value;
            }
        }
        return sum;
    }

    // ==================== IMPLEMENTATION METHODS ====================

    private static int DivideWithException(int numerator, int denominator)
    {
        if (denominator == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        return numerator / denominator;
    }

    private static FluentResults.Result<int> DivideWithFluentResults(int numerator, int denominator)
    {
        if (denominator == 0)
            return FluentResults.Result.Fail<int>("Cannot divide by zero");
        return FluentResults.Result.Ok(numerator / denominator);
    }

    private static Either<string, int> DivideWithLanguageExt(int numerator, int denominator)
    {
        if (denominator == 0)
            return Left<string, int>("Cannot divide by zero");
        return Right<string, int>(numerator / denominator);
    }

    private static Verdict.Result<int> DivideWithVerdict(int numerator, int denominator)
    {
        if (denominator == 0)
            return Verdict.Result<int>.Failure("DIVIDE_BY_ZERO", "Cannot divide by zero");
        return Verdict.Result<int>.Success(numerator / denominator);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<CompetitiveBenchmarks>();
    }
}
