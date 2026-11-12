using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Verdict.AspNetCore.Tests;

// No naming policy on purpose: the default HttpJsonOptions are camelCase, so
// PascalCase output proves the body went through this context's metadata.
[JsonSerializable(typeof(Gadget))]
internal partial class GadgetJsonContext : JsonSerializerContext
{
}

internal sealed record Gadget(int Id, string Name);

[Collection(ProblemDetailsStaticCollection.Name)]
public class JsonTypeInfoOverloadTests
{
    private static Task<(int Status, string? ContentType, string Body)> ExecuteAsync(IResult result) =>
        RunAsync(_ => result, null);

    private static async Task<(int Status, string? ContentType, string Body)> RunAsync(
        Func<HttpContext, IResult> make, Action<IServiceCollection>? configure)
    {
        var collection = new ServiceCollection().AddLogging();
        configure?.Invoke(collection);
        var services = collection.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        using var body = new MemoryStream();
        context.Response.Body = body;

        await make(context).ExecuteAsync(context);

        body.Position = 0;
        using var reader = new StreamReader(body);
        return (context.Response.StatusCode, context.Response.ContentType, await reader.ReadToEndAsync());
    }


    // The tests below pin the behaviour of the EXISTING overloads, which shipped in
    // 3.0.1 and must keep serialising reflectively with the configured
    // HttpJsonOptions. They run against the unannotated call shape on purpose.

    private sealed class RichProblem : ProblemDetails
    {
        public string? TraceHint { get; set; }
    }

    private sealed record Detail(int Code);

    private sealed class FixedFactory(ProblemDetails details) : IVerdictProblemDetailsFactory
    {
        public ProblemDetails CreateFromError(Error error, int statusCode = 400) => details;
    }

#pragma warning disable IL2026, IL3050
    [Fact]
    public async Task ExistingOverloadWritesTheExtraPropertiesOfASubclass()
    {
        var factory = new FixedFactory(new RichProblem { Status = 400, Title = "t", TraceHint = "abc" });

        var (_, _, body) = await RunAsync(
            ctx => Result<int>.Failure("E", "m").ToHttpResult(ctx, 200),
            c => c.AddSingleton<IVerdictProblemDetailsFactory>(factory));

        body.Should().Contain("\"traceHint\":\"abc\"");
    }

    [Fact]
    public async Task ExistingOverloadSerialisesAnObjectValuedExtension()
    {
        var details = new ProblemDetails { Status = 400, Title = "t" };
        details.Extensions["obj"] = new Detail(9);

        var (_, _, body) = await RunAsync(
            ctx => Result<int>.Failure("E", "m").ToHttpResult(ctx, 200),
            c => c.AddSingleton<IVerdictProblemDetailsFactory>(new FixedFactory(details)));

        body.Should().Contain("\"obj\":{\"code\":9}");
    }

    [Fact]
    public async Task ExistingOverloadAppliesTheConfiguredDictionaryKeyPolicy()
    {
        var details = new ValidationProblemDetails(new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["FirstName"] = new[] { "required" }
        }) { Status = 400, Title = "t" };

        var (_, _, body) = await RunAsync(
            ctx => Result<int>.Failure("E", "m").ToHttpResult(ctx, 200),
            c =>
            {
                c.AddSingleton<IVerdictProblemDetailsFactory>(new FixedFactory(details));
                c.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
                    o.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower);
            });

        body.Should().Contain("\"first_name\"");
    }
#pragma warning restore IL2026, IL3050

    [Fact]
    public async Task SuccessIsWrittenThroughTheSuppliedTypeInfo()
    {
        var result = Result<Gadget>.Success(new Gadget(3, "cog"));

        var (status, _, body) = await ExecuteAsync(result.ToHttpResult(GadgetJsonContext.Default.Gadget, 201));

        status.Should().Be(201);
        body.Should().Be("{\"Id\":3,\"Name\":\"cog\"}");
    }

    [Fact]
    public async Task ContextOverloadWritesSuccessThroughTheSuppliedTypeInfo()
    {
        var result = Result<Gadget>.Success(new Gadget(3, "cog"));

        var (status, _, body) = await ExecuteAsync(
            result.ToHttpResult(new DefaultHttpContext(), GadgetJsonContext.Default.Gadget));

        status.Should().Be(200);
        body.Should().Be("{\"Id\":3,\"Name\":\"cog\"}");
    }

    [Fact]
    public async Task BodylessStatusStillSendsNoBody()
    {
        var result = Result<Gadget>.Success(new Gadget(1, "x"));

        var (status, _, body) = await ExecuteAsync(result.ToHttpResult(GadgetJsonContext.Default.Gadget, 204));

        status.Should().Be(204);
        body.Should().BeEmpty();
    }

    [Fact]
    public void TypeInfoOverloadsRejectNull()
    {
        var result = Result<Gadget>.Success(new Gadget(1, "x"));

        var act = () => result.ToHttpResult((System.Text.Json.Serialization.Metadata.JsonTypeInfo<Gadget>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OnlyTheOverloadsWithoutTypeInfoCarryTheTrimAndAotAnnotations()
    {
        // Every overload that serialises with reflection must say so, or a trimmed
        // or AOT consumer finds out at run time instead of in the build. The
        // JsonTypeInfo overloads must not, or they would warn for no reason.
        var all = typeof(ResultExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(ResultExtensions.ToHttpResult))
            .ToList();
        var safe = all.Where(m => m.GetParameters().Any(p => p.ParameterType.Name.StartsWith("JsonTypeInfo", StringComparison.Ordinal))).ToList();
        var reflective = all.Except(safe).ToList();

        reflective.Should().HaveCount(5);
        safe.Should().HaveCount(3);
        foreach (var method in reflective)
        {
            method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>().Should().NotBeNull(method.ToString());
            method.GetCustomAttribute<RequiresDynamicCodeAttribute>().Should().NotBeNull(method.ToString());
        }

        foreach (var method in safe)
        {
            method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>().Should().BeNull(method.ToString());
        }
    }
}
