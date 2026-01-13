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

/// <summary>
/// Benchmarks for JSON serialization of Result types.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class JsonSerializationBenchmarks
{
    private const int Iterations = 1000;
    private static readonly System.Text.Json.JsonSerializerOptions _verdictOptions = 
        Verdict.Json.VerdictJsonExtensions.CreateVerdictJsonOptions();
    private static readonly System.Text.Json.JsonSerializerOptions _defaultOptions = 
        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    private readonly Verdict.Result<int> _verdictSuccess = Verdict.Result<int>.Success(42);
    private readonly Verdict.Result<int> _verdictFailure = Verdict.Result<int>.Failure("ERROR", "An error occurred");
    private readonly FluentResults.Result<int> _fluentSuccess = FluentResults.Result.Ok(42);
    private readonly FluentResults.Result<int> _fluentFailure = FluentResults.Result.Fail<int>("An error occurred");

    // ==================== SERIALIZATION ====================

    [Benchmark(Baseline = true, Description = "Verdict.Json Serialize (Success)")]
    public string VerdictJson_Serialize_Success()
    {
        return System.Text.Json.JsonSerializer.Serialize(_verdictSuccess, _verdictOptions);
    }

    [Benchmark(Description = "Verdict.Json Serialize (Failure)")]
    public string VerdictJson_Serialize_Failure()
    {
        return System.Text.Json.JsonSerializer.Serialize(_verdictFailure, _verdictOptions);
    }

    [Benchmark(Description = "Manual DTO Serialize (Success)")]
    public string ManualDto_Serialize_Success()
    {
        var dto = new { isSuccess = true, value = 42 };
        return System.Text.Json.JsonSerializer.Serialize(dto, _defaultOptions);
    }

    [Benchmark(Description = "Manual DTO Serialize (Failure)")]
    public string ManualDto_Serialize_Failure()
    {
        var dto = new { isSuccess = false, error = new { code = "ERROR", message = "An error occurred" } };
        return System.Text.Json.JsonSerializer.Serialize(dto, _defaultOptions);
    }

    // ==================== ROUND-TRIP ====================

    [Benchmark(Description = "Verdict.Json Round-Trip (Success)")]
    public Verdict.Result<int> VerdictJson_RoundTrip_Success()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_verdictSuccess, _verdictOptions);
        return System.Text.Json.JsonSerializer.Deserialize<Verdict.Result<int>>(json, _verdictOptions);
    }

    [Benchmark(Description = "Verdict.Json Round-Trip (Failure)")]
    public Verdict.Result<int> VerdictJson_RoundTrip_Failure()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_verdictFailure, _verdictOptions);
        return System.Text.Json.JsonSerializer.Deserialize<Verdict.Result<int>>(json, _verdictOptions);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Running Verdict Benchmarks...\n");
        
        // Run competitive benchmarks by default
        var summary = BenchmarkRunner.Run<CompetitiveBenchmarks>();
        
        // Optionally run JSON benchmarks
        if (args.Contains("--json"))
        {
            Console.WriteLine("\nRunning JSON Serialization Benchmarks...\n");
            BenchmarkRunner.Run<JsonSerializationBenchmarks>();
        }
    }
}
