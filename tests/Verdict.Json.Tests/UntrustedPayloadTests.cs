using System;
using System.Text.Json;
using Verdict.Json;
using Xunit;

namespace Verdict.Json.Tests;

/// <summary>
/// The converters read bytes the process did not write, which is the one place
/// in this library where the input is untrusted by definition.
/// </summary>
/// <remarks>
/// The invariant every case below asserts: a payload either produces a valid
/// <see cref="Result{T}" /> or throws <see cref="JsonException" />, and no
/// success ever carries <c>default(T)</c> unless the payload said so. The
/// success branch used to skip the check the failure branch performed, so a
/// truncated body became a success carrying null.
/// </remarks>
public class UntrustedPayloadTests
{
    private static readonly JsonSerializerOptions Options =
        VerdictJsonExtensions.CreateVerdictJsonOptions();

    [Theory]
    [InlineData("{\"isSuccess\":true}")]
    [InlineData("{\"isSuccess\":true,\"error\":{\"code\":\"E\",\"message\":\"m\"}}")]
    [InlineData("{\"isSuccess\":true,\"unrelated\":1}")]
    public void ASuccessWithNoValueIsRejected(string payload)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result<int>>(payload, Options));
    }

    [Fact]
    public void ASuccessWithNoValueIsRejectedForAReferenceType()
    {
        // The case that mattered most: this used to return IsSuccess=true with a
        // null Value, and the null went on to be dereferenced somewhere else.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Result<Uri>>("{\"isSuccess\":true}", Options));
    }

    [Fact]
    public void AnExplicitNullValueIsStillAccepted()
    {
        // Different from the property being absent. The payload said null, so the
        // caller asked for a success carrying null and gets one.
        var result = JsonSerializer.Deserialize<Result<string>>(
            "{\"isSuccess\":true,\"value\":null}", Options);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void AFailureWithNoErrorIsRejected()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Result<int>>("{\"isSuccess\":false}", Options));
    }

    [Fact]
    public void APayloadWithNoIsSuccessIsRejected()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Result<int>>("{\"value\":42}", Options));
    }

    [Theory]
    [InlineData("{\"isSuccess\":true,\"value\":42")]
    [InlineData("{\"isSuccess\":")]
    [InlineData("{")]
    public void ATruncatedPayloadIsRejected(string payload)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result<int>>(payload, Options));
    }

    [Fact]
    public void AWrongTypedValueIsRejected()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Result<int>>("{\"isSuccess\":true,\"value\":\"forty two\"}", Options));
    }

    [Fact]
    public void ADuplicateValueTakesTheLastOneAndStaysASuccess()
    {
        // System.Text.Json allows duplicate properties by default. What matters
        // is that the outcome is a well-formed result rather than a half-read one.
        var result = JsonSerializer.Deserialize<Result<int>>(
            "{\"isSuccess\":true,\"value\":1,\"value\":42}", Options);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ADeeplyNestedValueIsRejectedRatherThanCrashing()
    {
        var nested = new string('[', 200) + new string(']', 200);

        Assert.ThrowsAny<JsonException>(() =>
            JsonSerializer.Deserialize<Result<int>>(
                $"{{\"isSuccess\":true,\"value\":{nested}}}", Options));
    }

    [Fact]
    public void AValidPayloadStillRoundTrips()
    {
        // The fix must not make well-formed payloads fail.
        foreach (var original in new[] { Result<int>.Success(42), Result<int>.Failure("E_CODE", "m") })
        {
            var json = JsonSerializer.Serialize(original, Options);
            var restored = JsonSerializer.Deserialize<Result<int>>(json, Options);

            Assert.Equal(original.IsSuccess, restored.IsSuccess);
            if (original.IsSuccess)
            {
                Assert.Equal(original.Value, restored.Value);
            }
            else
            {
                Assert.Equal(original.Error.Code, restored.Error.Code);
            }
        }
    }
}
