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
        // A failure never touches jsonTypeInfo, so only the explicit check can throw.
        var ok = Result<Gadget>.Failure("E", "m");
        var plain = Result.Success();
        var ctx = new DefaultHttpContext();
        var info = GadgetJsonContext.Default.Gadget;

        var nullInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<Gadget>)null!;
        var nullContext = (HttpContext)null!;

        new Action(() => ok.ToHttpResult(nullInfo)).Should().Throw<ArgumentNullException>().WithParameterName("jsonTypeInfo");
        new Action(() => ok.ToHttpResult(ctx, nullInfo)).Should().Throw<ArgumentNullException>().WithParameterName("jsonTypeInfo");
        new Action(() => ok.ToHttpResult(nullContext, info)).Should().Throw<ArgumentNullException>().WithParameterName("context");
        new Action(() => plain.ToHttpResult(nullContext, null)).Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    // ---- error path through the three JsonTypeInfo overloads ----

    // Writes a property only this metadata knows about, so a body containing it
    // proves the caller's JsonTypeInfo was used and not the built-in one.
    private sealed class MarkerProblemConverter : JsonConverter<ProblemDetails>
    {
        public override ProblemDetails? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, ProblemDetails value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("marker", "custom-" + value.Status);
            writer.WriteEndObject();
        }
    }

    private static System.Text.Json.Serialization.Metadata.JsonTypeInfo<ProblemDetails> CustomProblemInfo() =>
        (System.Text.Json.Serialization.Metadata.JsonTypeInfo<ProblemDetails>)new JsonSerializerOptions
        {
            Converters = { new MarkerProblemConverter() },
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        }.GetTypeInfo(typeof(ProblemDetails));

    private static Func<HttpContext, IResult>[] ErrorOverloads(
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<ProblemDetails>? problemInfo) => new Func<HttpContext, IResult>[]
    {
        _ => Result<Gadget>.Failure("E", "m").ToHttpResult(GadgetJsonContext.Default.Gadget, 200, null, problemInfo),
        ctx => Result<Gadget>.Failure("E", "m").ToHttpResult(ctx, GadgetJsonContext.Default.Gadget, 200, problemInfo),
        ctx => Result.Failure("E", "m").ToHttpResult(ctx, problemInfo),
    };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ErrorBodyUsesTheSuppliedProblemTypeInfo(int overload)
    {
        var make = ErrorOverloads(CustomProblemInfo())[overload];

        var (status, contentType, body) = await RunAsync(make, null);

        body.Should().Be("{\"marker\":\"custom-" + status + "\"}");
        contentType.Should().Be("application/problem+json");
    }

    // Overload 0 takes no HttpContext, so it cannot see a registered factory.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ValidationProblemDetailsKeepsItsErrors(int overload)
    {
        var details = new ValidationProblemDetails(new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["Name"] = new[] { "required" }
        }) { Status = 422, Title = "t" };

        var (status, contentType, body) = await RunAsync(
            ErrorOverloads(null)[overload],
            c => c.AddSingleton<IVerdictProblemDetailsFactory>(new FixedFactory(details)));

        status.Should().Be(400);
        contentType.Should().Be("application/problem+json");
        body.Should().Contain("\"errors\":{\"Name\":[\"required\"]}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ErrorPathSetsStatusAndContentType(int overload)
    {
        var (status, contentType, body) = await RunAsync(ErrorOverloads(null)[overload], null);

        status.Should().Be(400);
        contentType.Should().Be("application/problem+json");
        body.Should().Contain("\"status\":400");
    }

    public static TheoryData<string, object> ExtensionValues => new()
    {
        { "long", 5_000_000_000L },
        { "bool", true },
        { "double", 1.5d },
        { "decimal", 2.25m },
        { "guid", Guid.Parse("11111111-2222-3333-4444-555555555555") },
        { "datetime", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc) },
        { "datetimeoffset", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero) },
        { "int", 7 },
        { "intarray", new[] { 1, 2 } },
        { "stringarray", new[] { "a", "b" } },
        { "string", "s" },
    };

    [Theory]
    [MemberData(nameof(ExtensionValues))]
    public async Task BuiltInContextWritesPrimitiveExtensionValues(string name, object value)
    {
        var details = new ProblemDetails { Status = 400, Title = "t" };
        details.Extensions["k"] = value;

        var (status, _, body) = await RunAsync(
            ctx => Result<Gadget>.Failure("E", "m").ToHttpResult(ctx, GadgetJsonContext.Default.Gadget),
            c => c.AddSingleton<IVerdictProblemDetailsFactory>(new FixedFactory(details)));

        status.Should().Be(400, name);
        var expected = JsonSerializer.Serialize(value);
        body.Should().Contain("\"k\":" + expected, name);
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
