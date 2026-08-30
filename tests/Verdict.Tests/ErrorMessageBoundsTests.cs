using System;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// The error code was validated and the message was not, which is backwards.
/// </summary>
/// <remarks>
/// A code is chosen by the programmer. A message is where a username, a filename
/// or a request value gets interpolated, so it is the field that carries
/// attacker-influenced text, and it is the field written into the log. A carriage
/// return in it forges a line in any plain-text sink.
/// </remarks>
public class ErrorMessageBoundsTests
{
    [Fact]
    public void AControlCharacterCannotForgeALogLine()
    {
        var forged = "user not found\r\n2026-08-31 00:00:00 [INF] Admin login succeeded for 'root'";

        var error = new Error("NOT_FOUND", forged);

        Assert.DoesNotContain("\r", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", error.Message, StringComparison.Ordinal);
        Assert.Contains("user not found", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData('\u0000')]
    [InlineData('\u0007')]
    [InlineData('\u001b')]
    [InlineData('\u007f')]
    public void OtherControlCharactersAreRemovedToo(char control)
    {
        var error = new Error("E", $"before{control}after");

        Assert.DoesNotContain(control.ToString(), error.Message, StringComparison.Ordinal);
        Assert.Equal("before after", error.Message);
    }

    [Fact]
    public void ATabIsLeftAlone()
    {
        // Tabs are ordinary in a message and do not start a new log line.
        var error = new Error("E", "column\tvalue");

        Assert.Equal("column\tvalue", error.Message);
    }

    [Fact]
    public void AnOversizedMessageIsTruncatedAndSaysSo()
    {
        var error = new Error("E", new string('A', 5_000_000));

        Assert.Equal(Error.MaxMessageLength + Error.TruncationMarker.Length, error.Message.Length);
        Assert.EndsWith(Error.TruncationMarker, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMessageAtTheLimitIsLeftWhole()
    {
        var exact = new string('A', Error.MaxMessageLength);

        var error = new Error("E", exact);

        Assert.Equal(exact, error.Message);
    }

    [Fact]
    public void AnOrdinaryMessageIsReturnedUnchangedAndUncopied()
    {
        var message = "the account is already registered";

        var error = new Error("DUPLICATE", message);

        // Same reference, so the clean path allocates nothing. The allocation
        // gate asserts the byte count; this says why it stays zero.
        Assert.Same(message, error.Message);
    }

    [Fact]
    public void ANullMessageIsStillEmptyRatherThanNull()
    {
        var error = new Error("E", null!);

        Assert.Equal(string.Empty, error.Message);
    }
}
