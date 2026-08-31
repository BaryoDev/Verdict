using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Verdict.Extensions;
using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// Asserts what a default-configured pipeline does, rather than what it can be
/// made to do.
/// </summary>
/// <remarks>
/// The existing factory tests all construct their error with a code that maps to
/// 500, which is the one path where message suppression worked. Nothing walked
/// the path where it did not, so the defaults leaked for four releases while the
/// suite stayed green.
/// <para>
/// Every assertion here is on the serialised body as a string. Inspecting
/// <see cref="ProblemDetails.Detail"/> alone misses a leak that arrives through
/// an extension, which is exactly how the error code smuggled the exception type.
/// </para>
/// </remarks>
[Collection(ProblemDetailsStaticCollection.Name)]
public class SecureDefaultsTests : IDisposable
{
    private const string Secret = "Server=prod-sql-03.internal;Password=hunter2";

    private static readonly InvalidOperationException DatabaseFailure = new(
        "Login failed for user 'svc_billing'. " + Secret);

    public SecureDefaultsTests() => ProblemDetailsFactory.ResetDefaultOptions();

    public void Dispose() => ProblemDetailsFactory.ResetDefaultOptions();

    // Serialised as object, so a ValidationProblemDetails keeps its Errors
    // dictionary. Declaring the parameter as ProblemDetails silently drops it.
    private static string Body(ProblemDetails details) => JsonSerializer.Serialize<object>(details);

    private static string BodyFor(Error error, VerdictProblemDetailsOptions? options = null)
    {
        if (options is not null)
        {
            ProblemDetailsFactory.SetDefaultOptions(options);
        }

        var status = ErrorStatusCodeMapper.GetStatusCode(error);
        return Body(ProblemDetailsFactory.CreateFromError(error, status));
    }

    [Fact]
    public void ExceptionDerivedErrorIsReportedAsAServerFailure()
    {
        var error = Error.FromException(DatabaseFailure, sanitize: false);

        // 400 asserts the caller made a mistake, and nothing here established that.
        // It also kept these out of 5xx alerting entirely.
        Assert.Equal(500, ErrorStatusCodeMapper.GetStatusCode(error));
    }

    [Fact]
    public void DefaultsDoNotPutTheExceptionMessageInTheBody()
    {
        var body = BodyFor(Error.FromException(DatabaseFailure, sanitize: false));

        Assert.DoesNotContain("hunter2", body, StringComparison.Ordinal);
        Assert.DoesNotContain("svc_billing", body, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludeErrorMessageFalseSuppressesTheExceptionMessage()
    {
        // The case the option exists for, and the one it could not reach before.
        var body = BodyFor(
            Error.FromException(DatabaseFailure, sanitize: false),
            new VerdictProblemDetailsOptions { IncludeErrorMessage = false });

        Assert.DoesNotContain("hunter2", body, StringComparison.Ordinal);
        Assert.Contains("An unexpected error occurred.", body, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultsDoNotNameTheExceptionType()
    {
        var body = BodyFor(Error.FromException(DatabaseFailure, sanitize: true));

        // IncludeExceptionDetails is off by default, so neither the dedicated
        // extension nor the error code may carry the type name.
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultsDoNotIncludeAStackTrace()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException caught)
        {
            var body = BodyFor(Error.FromException(caught, sanitize: true));

            Assert.DoesNotContain("stackTrace", body, StringComparison.Ordinal);
            Assert.DoesNotContain(nameof(DefaultsDoNotIncludeAStackTrace), body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AHandWrittenErrorStillReportsItsOwnMessageAndCode()
    {
        // The fix must not silence errors that were written for a client to read.
        var body = BodyFor(new Error("DUPLICATE_EMAIL", "that email is already registered"));

        Assert.Contains("that email is already registered", body, StringComparison.Ordinal);
        Assert.Contains("DUPLICATE_EMAIL", body, StringComparison.Ordinal);
        Assert.Equal(409, ErrorStatusCodeMapper.GetStatusCode(new Error("DUPLICATE_EMAIL", "x")));
    }

    [Fact]
    public void TurningExceptionDetailsOnStillWorksForDevelopment()
    {
        var body = BodyFor(
            Error.FromException(DatabaseFailure, sanitize: false),
            new VerdictProblemDetailsOptions { IncludeExceptionDetails = true });

        Assert.Contains("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.Contains("hunter2", body, StringComparison.Ordinal);
    }

    [Fact]
    public void ADisposedMultiResultProducesAResponseRatherThanAnException()
    {
        var collection = ErrorCollection.Create((IEnumerable<Error>)new List<Error>
        {
            new("INVALID_AGE", "age cannot be negative"),
            new("INVALID_NAME", "name is required"),
        });
        var result = MultiResult<int>.Failure(collection);
        result.DisposeErrors();

        // Throwing here would turn a reported validation failure into an
        // unhandled 500, from inside the code building the error response.
        var details = ProblemDetailsFactory.CreateFromMultiResult(result);

        Assert.Equal(400, details.Status);
        Assert.NotNull(details.Title);
    }

    [Fact]
    public void ALiveMultiResultStillReportsItsErrors()
    {
        var result = MultiResult<int>.Failure(
            new Error("INVALID_AGE", "age cannot be negative"),
            new Error("INVALID_NAME", "name is required"));

        var body = Body(ProblemDetailsFactory.CreateFromMultiResult(result));

        Assert.Contains("age cannot be negative", body, StringComparison.Ordinal);
        Assert.Contains("name is required", body, StringComparison.Ordinal);
    }
}
