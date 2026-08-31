using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace VerdictBenchmarks.Field;

/// <summary>
/// The CSharpFunctionalExtensions half of the field comparison.
/// </summary>
/// <remarks>
/// In its own file for the same reason as the ErrorOr one: its extension methods
/// need the namespace imported, and <c>Result</c> then collides with Verdict's.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class CfeFieldBenchmarks
{
    private const int Iterations = 1000;
    private const int AsyncIterations = 100;

    private static readonly Dictionary<int, string> Lookup =
        Enumerable.Range(0, 128).ToDictionary(i => i, i => $"item-{i}");

    private static string Describe(int value) =>
        Lookup.TryGetValue(value & 127, out var name) && name.StartsWith("item", StringComparison.Ordinal)
            ? name
            : "unknown";

    private static int Double(int x) => x * 2;

    [Benchmark(Description = "CSharpFunctionalExtensions success")]
    public int Success()
    {
        var sum = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var result = Result.Success<int>(i);
            sum += result.IsSuccess ? result.Value : 0;
        }
        return sum;
    }

    [Benchmark(Description = "CSharpFunctionalExtensions failure")]
    public int Failure()
    {
        var count = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var result = Result.Failure<int>("missing");
            if (result.IsFailure && result.Error.Length > 0) count++;
        }
        return count;
    }

    [Benchmark(Description = "CSharpFunctionalExtensions chain with work")]
    public int Chain()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += Result.Success<int>(i).Map(Describe).Match(name => name.Length, _ => -1);
        }
        return total;
    }

    [Benchmark(Description = "CSharpFunctionalExtensions async, four steps")]
    public int AsyncChain()
    {
        var total = 0;
        for (var i = 0; i < AsyncIterations; i++)
        {
            var pending = Task.FromResult(Result.Success<int>(i))
                .Map(Double).Map(Double).Map(Double).Map(Double);
            total += pending.Result is var result && result.IsSuccess ? 1 : 0;
        }
        return total;
    }
}
