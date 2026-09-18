using System;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Gates the sanitisation promise at the point it was being broken.
/// </summary>
/// <remarks>
/// <c>Error</c> is a record struct, so before this the compiler generated a
/// <c>ToString</c> that printed every property including the exception, and an
/// exception prints its own message. A caller who followed the documented advice
/// and sanitised got the original message back the moment anything logged the
/// error value, which is the one thing sanitising is for.
/// </remarks>
public class ErrorToStringTests
{
    private const string Secret = "Server=prod-sql-03.internal;Password=hunter2";

    [Fact]
    public void SanitizedErrorDoesNotPrintTheOriginalMessage()
    {
        var error = Error.FromException(new InvalidOperationException(Secret), sanitize: true);

        var text = error.ToString();

        Assert.DoesNotContain("hunter2", text, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, text, StringComparison.Ordinal);
        Assert.Contains("An error occurred.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitizedErrorInAnInterpolatedStringDoesNotLeakEither()
    {
        var error = Error.FromException(new InvalidOperationException(Secret), sanitize: true);

        // The shape a logger or a debugger actually produces.
        var text = $"{error}";

        Assert.DoesNotContain("hunter2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsanitizedErrorStillDoesNotPrintTheExceptionItself()
    {
        // Even without sanitising, ToString prints the message the caller chose,
        // not the exception's own rendering with its stack.
        var error = new Error("DB_ERROR", "the database rejected the write",
            new InvalidOperationException(Secret));

        var text = error.ToString();

        Assert.DoesNotContain("hunter2", text, StringComparison.Ordinal);
        Assert.Equal("[DB_ERROR] the database rejected the write (+InvalidOperationException)", text);
    }

    [Fact]
    public void PlainErrorReadsAsCodeAndMessage()
    {
        var error = new Error("NOT_FOUND", "missing");

        Assert.Equal("[NOT_FOUND] missing", error.ToString());
    }

    [Fact]
    public void ExceptionIsStillReachableForCallersThatWantIt()
    {
        var cause = new InvalidOperationException(Secret);
        var error = Error.FromException(cause, sanitize: true);

        Assert.Same(cause, error.Exception);
    }
}
