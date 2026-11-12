using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verdict.AspNetCore;
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

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Widget))]
internal partial class WidgetJsonContext : JsonSerializerContext
{
}

internal sealed record Widget(int Id, string Name);

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

        await CheckHttpResultsAsync();

        Console.WriteLine(_failures == 0 ? "AOT smoke passed" : $"AOT smoke failed: {_failures}");
        return _failures;
    }

    // Runs the AspNetCore overloads that take a JsonTypeInfo, and the error path,
    // which goes through the package's own source-generated context. Each result
    // is executed against a real response body, so a missing metadata entry fails
    // here at run time and not only in a consumer's production.
    private static async Task CheckHttpResultsAsync()
    {
        var ok = Result<Widget>.Success(new Widget(7, "gear"));
        Check("ToHttpResult typeInfo success",
            await BodyAsync(ok.ToHttpResult(WidgetJsonContext.Default.Widget)) == "200 {\"id\":7,\"name\":\"gear\"}");

        var failed = Result<Widget>.Failure(new Error("NOT_FOUND", "no such widget"));
        var problem = await BodyAsync(failed.ToHttpResult(WidgetJsonContext.Default.Widget));
        Check("ToHttpResult typeInfo error is a problem document",
            problem.StartsWith("404 ", StringComparison.Ordinal)
            && problem.Contains("\"detail\":\"no such widget\"", StringComparison.Ordinal)
            && problem.Contains("\"errorCode\":\"NOT_FOUND\"", StringComparison.Ordinal));

        var context = new DefaultHttpContext();
        Check("ToHttpResult context typeInfo success",
            await BodyAsync(ok.ToHttpResult(context, WidgetJsonContext.Default.Widget)) == "200 {\"id\":7,\"name\":\"gear\"}");

        var plain = await BodyAsync(Result.Failure(new Error("CONFLICT", "taken")).ToHttpResult(context, null));
        Check("ToHttpResult non-generic error (no reflection)",
            plain.StartsWith("409 ", StringComparison.Ordinal)
            && plain.Contains("\"detail\":\"taken\"", StringComparison.Ordinal));
    }

    private static async Task<string> BodyAsync(IResult result)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        using var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);

        body.Position = 0;
        using var reader = new StreamReader(body);
        return $"{context.Response.StatusCode} {await reader.ReadToEndAsync()}";
    }
}
