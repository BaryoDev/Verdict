using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Verdict.Extensions;
using Verdict.Fluent;
using Verdict.Json;
using Xunit;

namespace Verdict.NetStandard.Tests;

/// <summary>
/// Exercises the netstandard2.0 assemblies, which ship to NuGet and which no
/// test had ever loaded.
/// </summary>
public class NetStandardAssetTests
{
    /// <summary>
    /// Guards the point of this project. Without it every test below would still
    /// pass while quietly running the net8.0 assets, which is a gate that cannot
    /// fail.
    /// </summary>
    [Fact]
    public void TheAssembliesUnderTestAreTheNetStandardOnes()
    {
        foreach (var assembly in new[]
                 {
                     typeof(Result<>).Assembly,
                     typeof(ErrorCollection).Assembly,
                     typeof(VerdictJsonExtensions).Assembly,
                 })
        {
            // The file path is the test project's output directory whichever asset
            // was copied there, so it proves nothing. The compiler stamps this
            // attribute with the framework the assembly was actually built for.
            var stamped = assembly
                .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()
                ?.FrameworkName;

            Assert.True(
                stamped is not null && stamped.StartsWith(".NETStandard", StringComparison.Ordinal),
                $"{assembly.GetName().Name} was built for {stamped ?? "an unstamped framework"}, "
                + "not netstandard2.0. SetTargetFramework in the csproj is not taking effect, so "
                + "this project is testing the same assets as every other test project.");
        }
    }

    [Fact]
    public void TheHandWrittenGetHashCodeWorks()
    {
        // This exists because System.HashCode is unavailable on netstandard2.0
        // without an extra dependency, and until now it only ever ran on net8.0.
        var first = Result<int>.Success(42);
        var same = Result<int>.Success(42);
        var different = Result<int>.Success(43);

        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first.GetHashCode(), different.GetHashCode());
    }

    [Fact]
    public void GetHashCodeWorksForAReferenceTypedValue()
    {
        var first = Result<string>.Success("hello");
        var same = Result<string>.Success("hello");

        Assert.Equal(first.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void GetHashCodeWorksForAFailure()
    {
        var error = new Error("NOT_FOUND", "missing");

        Assert.Equal(
            Result<int>.Failure(error).GetHashCode(),
            Result<int>.Failure(error).GetHashCode());
    }

    [Fact]
    public void ResultsWorkInAHashSet()
    {
        var set = new HashSet<Result<int>>
        {
            Result<int>.Success(1),
            Result<int>.Success(1),
            Result<int>.Success(2),
        };

        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void TheFluentChainWorks()
    {
        var value = Result<int>.Success(20)
            .Map(x => x + 1)
            .Bind(x => Result<int>.Success(x * 2))
            .Match(x => x, _ => -1);

        Assert.Equal(42, value);
    }

    [Fact]
    public void ArrayPoolBackedCollectionsWork()
    {
        // Verdict.Extensions pulls System.Memory for ArrayPool on this target, so
        // this is a different implementation from the net8.0 one.
        var errors = Enumerable.Range(0, ErrorCollection.PoolingThreshold + 4)
            .Select(i => new Error($"E{i}", $"message {i}"))
            .ToList();

        var collection = ErrorCollection.Create((IEnumerable<Error>)errors);

        Assert.Equal(errors.Count, collection.Count);
        Assert.Equal("E0", collection.First().Code);

        collection.Dispose();
        Assert.True(collection.IsDisposed);
    }

    [Fact]
    public void MessageNormalisationWorksHereToo()
    {
        var error = new Error("E", "line one\r\nline two");

        Assert.DoesNotContain("\n", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonRoundTripsOnTheNonGeneratedPath()
    {
        // Verdict.Json is the one project with #if NET8_0_OR_GREATER blocks, so
        // its two builds are genuinely different code.
        var options = VerdictJsonExtensions.CreateVerdictJsonOptions();

        var json = JsonSerializer.Serialize(Result<int>.Success(42), options);
        var restored = JsonSerializer.Deserialize<Result<int>>(json, options);

        Assert.True(restored.IsSuccess);
        Assert.Equal(42, restored.Value);
    }

    [Fact]
    public void JsonStillRejectsASuccessWithNoValue()
    {
        var options = VerdictJsonExtensions.CreateVerdictJsonOptions();

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Result<int>>("{\"isSuccess\":true}", options));
    }
}
