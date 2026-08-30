using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace VerdictBenchmarks.Field;

/// <summary>
/// The ErrorOr half of the field comparison.
/// </summary>
/// <remarks>
/// In its own file because its extension methods only resolve with the
/// <c>ErrorOr</c> namespace imported, and importing that alongside
/// <c>Verdict</c> makes <c>Error</c> and <c>Result</c> ambiguous.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ErrorOrFieldBenchmarks
{
    private const int Iterations = 1000;
    private const int AsyncIterations = 100;

    private static readonly Error NotFound = Error.NotFound("NOT_FOUND", "missing");

    private static readonly Dictionary<int, string> Lookup =
        Enumerable.Range(0, 128).ToDictionary(i => i, i => $"item-{i}");

    private static string Describe(int value) =>
        Lookup.TryGetValue(value & 127, out var name) && name.StartsWith("item", StringComparison.Ordinal)
            ? name
            : "unknown";

    private static int Double(int x) => x * 2;

    [Benchmark(Description = "ErrorOr success")]
    public int Success()
    {
        var sum = 0;
        for (var i = 0; i < Iterations; i++)
        {
            ErrorOr<int> result = i;
            sum += result.IsError ? 0 : result.Value;
        }
        return sum;
    }

    [Benchmark(Description = "ErrorOr failure")]
    public int Failure()
    {
        var count = 0;
        for (var i = 0; i < Iterations; i++)
        {
            ErrorOr<int> result = NotFound;
            if (result.IsError && result.FirstError.Code.Length > 0) count++;
        }
        return count;
    }

    [Benchmark(Description = "ErrorOr chain with work")]
    public int Chain()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            ErrorOr<int> start = i;
            total += start.Then(Describe).Match(name => name.Length, _ => -1);
        }
        return total;
    }

    [Benchmark(Description = "ErrorOr async, four steps")]
    public async Task<int> AsyncChain()
    {
        var total = 0;
        for (var i = 0; i < AsyncIterations; i++)
        {
            ErrorOr<int> start = i;
            var result = await Task.FromResult(start)
                .ThenAsync(x => Task.FromResult(Double(x)))
                .ThenAsync(x => Task.FromResult(Double(x)))
                .ThenAsync(x => Task.FromResult(Double(x)))
                .ThenAsync(x => Task.FromResult(Double(x)));
            total += result.IsError ? 0 : 1;
        }
        return total;
    }
}
