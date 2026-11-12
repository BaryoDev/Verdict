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
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Verdict.AspNetCore.Tests;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Gadget))]
internal partial class GadgetJsonContext : JsonSerializerContext
{
}

internal sealed record Gadget(int Id, string Name);

[Collection(ProblemDetailsStaticCollection.Name)]
public class JsonTypeInfoOverloadTests
{
    private static async Task<(int Status, string? ContentType, string Body)> ExecuteAsync(IResult result)
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        using var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);

        body.Position = 0;
        using var reader = new StreamReader(body);
        return (context.Response.StatusCode, context.Response.ContentType, await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task SuccessIsWrittenThroughTheSuppliedTypeInfo()
    {
        var result = Result<Gadget>.Success(new Gadget(3, "cog"));

        var (status, _, body) = await ExecuteAsync(result.ToHttpResult(GadgetJsonContext.Default.Gadget, 201));

        status.Should().Be(201);
        body.Should().Be("{\"id\":3,\"name\":\"cog\"}");
    }

    [Fact]
    public async Task ContextOverloadWritesSuccessThroughTheSuppliedTypeInfo()
    {
        var result = Result<Gadget>.Success(new Gadget(3, "cog"));

        var (status, _, body) = await ExecuteAsync(
            result.ToHttpResult(new DefaultHttpContext(), GadgetJsonContext.Default.Gadget));

        status.Should().Be(200);
        body.Should().Be("{\"id\":3,\"name\":\"cog\"}");
    }

    [Fact]
    public async Task ErrorIsAProblemDocumentOnBothTypeInfoOverloads()
    {
        var result = Result<Gadget>.Failure("NOT_FOUND", "no such gadget");

        var viaOptions = await ExecuteAsync(result.ToHttpResult(GadgetJsonContext.Default.Gadget));
        var viaContext = await ExecuteAsync(
            result.ToHttpResult(new DefaultHttpContext(), GadgetJsonContext.Default.Gadget));

        foreach (var (status, contentType, body) in new[] { viaOptions, viaContext })
        {
            status.Should().Be(404);
            contentType.Should().Be("application/problem+json");
            using var doc = JsonDocument.Parse(body);
            doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
            doc.RootElement.GetProperty("detail").GetString().Should().Be("no such gadget");
            doc.RootElement.GetProperty("errorCode").GetString().Should().Be("NOT_FOUND");
        }
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
    public void ReflectiveOverloadsCarryTheTrimAndAotAnnotations()
    {
        // The overloads that serialise T with reflection must say so, or a trimmed
        // or AOT consumer finds out at run time instead of in the build.
        var reflective = typeof(ResultExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(ResultExtensions.ToHttpResult) && m.IsGenericMethodDefinition)
            .Where(m => !m.GetParameters().Any(p => p.ParameterType.Name.StartsWith("JsonTypeInfo", StringComparison.Ordinal)))
            .ToList();

        reflective.Should().HaveCount(3);
        foreach (var method in reflective)
        {
            method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>().Should().NotBeNull(method.ToString());
            method.GetCustomAttribute<RequiresDynamicCodeAttribute>().Should().NotBeNull(method.ToString());
        }

        var safe = typeof(ResultExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(ResultExtensions.ToHttpResult) && !reflective.Contains(m));
        foreach (var method in safe)
        {
            method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>().Should().BeNull(method.ToString());
        }
    }
}
