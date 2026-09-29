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
    [InlineData('\u0085')]
    [InlineData('\u009b')]
    [InlineData('\u2028')]
    [InlineData('\u2029')]
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

    [Theory]
    [InlineData(4097)]
    [InlineData(10_000)]
    [InlineData(5_000_000)]
    public void AnOversizedMessageIsTruncatedAndSaysSo(int length)
    {
        var error = new Error("E", new string('A', length));

        // Inside the bound, marker included. Truncating to MaxMessageLength and
        // then appending the marker overshot it by the marker's length, which
        // makes the constant a suggestion rather than a limit.
        Assert.Equal(Error.MaxMessageLength, error.Message.Length);
        Assert.True(error.Message.Length <= Error.MaxMessageLength);
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

    [Fact]
    public void TruncationDoesNotSplitASurrogatePair()
    {
        // The cut used to land between the two halves of an emoji, leaving a lone
        // high surrogate that an encoder turns into U+FFFD or rejects.
        var cut = Error.MaxMessageLength - Error.TruncationMarker.Length;
        var text = new string('A', cut - 1) + "\U0001F600" + new string('B', 100);

        var error = new Error("E", text);

        var kept = error.Message.Substring(0, error.Message.Length - Error.TruncationMarker.Length);
        Assert.False(char.IsHighSurrogate(kept[kept.Length - 1]));
        Assert.EndsWith(Error.TruncationMarker, error.Message, StringComparison.Ordinal);
    }
}
