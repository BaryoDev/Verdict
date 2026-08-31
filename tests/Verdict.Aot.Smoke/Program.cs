using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Verdict;
using Verdict.Async;
using Verdict.Extensions;
using Verdict.Fluent;
using Verdict.Json;
using Verdict.Rich;

namespace Verdict.Aot.Smoke;

/// <summary>
/// The AOT claim, as a thing that runs rather than a paragraph in the README.
/// </summary>
/// <remarks>
/// The README said a PublishAot console app using Result, JSON round-trips and
/// HashSet compiles and runs as a 3.3 MB native binary. Nothing in the repository
/// reproduced it, so the claim rested on a manual experiment nobody could repeat.
/// This is that experiment, checked in.
/// <para>
/// It uses the JsonTypeInfo overload rather than the JsonSerializerOptions one.
/// The options overload carries RequiresDynamicCode whatever converters are
/// registered on it, so following the README's snippet literally produced four
/// IL warnings and a consumer with TreatWarningsAsErrors could not build it.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(Converters = new[] { typeof(ResultJsonConverter<int>) })]
[JsonSerializable(typeof(Result<int>))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

public static class Program
{
    private static int _failures;

    private static void Check(string what, bool condition)
    {
        if (condition)
        {
            Console.WriteLine($"ok    {what}");
            return;
        }

        Console.Error.WriteLine($"FAIL  {what}");
        _failures++;
    }

    public static async Task<int> Main()
    {
        var mapped = Result<int>.Success(21).Map(static x => x * 2);
        Check("Fluent Map", mapped is { IsSuccess: true, Value: 42 });

        Check("Match", mapped.Match(static x => x, static _ => -1) == 42);

        // Generic virtual dispatch over a struct, which is the shape most likely
        // to need runtime code generation.
        var set = new HashSet<Result<int>> { mapped, Result<int>.Success(42) };
        Check("HashSet dedupe", set.Count == 1);

        var json = JsonSerializer.Serialize(mapped, AppJsonContext.Default.ResultInt32);
        var restored = JsonSerializer.Deserialize(json, AppJsonContext.Default.ResultInt32);
        Check("JSON round trip", json == "{\"isSuccess\":true,\"value\":42}" && restored.Value == 42);

        var multi = MultiResult<int>.Failure(new Error("A", "a"), new Error("B", "b"));
        Check("MultiResult", multi.ErrorCount == 2);

        var rich = Result<int>.Success(1).WithSuccess("done");
        Check("RichResult metadata", rich.Successes.Count == 1);

        var asyncResult = await Result<int>.Success(20)
            .AsValueTask()
            .Map(static x => x + 1)
            .Bind(static x => Result<int>.Success(x * 2));
        Check("ValueTask pipeline", asyncResult.Value == 42);

        var sanitized = Error.FromException(new InvalidOperationException("secret"), sanitize: true);
        Check("Sanitised error hides the cause", !sanitized.ToString().Contains("secret", StringComparison.Ordinal));

        Console.WriteLine(_failures == 0 ? "AOT smoke passed" : $"AOT smoke failed: {_failures}");
        return _failures;
    }
}
