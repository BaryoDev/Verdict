using System;
using FluentAssertions;
using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// Tests for VerdictProblemDetailsOptions configuration.
/// </summary>
[Collection(ProblemDetailsStaticCollection.Name)]
public class VerdictProblemDetailsOptionsTests : IDisposable
{
    // Teardown rather than an inline cleanup line. An inline restore only runs when the
    // assertions above it pass, so a failing test used to leave the custom defaults behind.
    // That mattered less while classes raced; now that this collection serialises them, leaked
    // state reaches the next class in the collection deterministically.
    public void Dispose() => ProblemDetailsFactory.ResetDefaultOptions();

    [Fact]
    public void CreateFromError_WithDefaultOptions_ShouldIncludeErrorCode()
    {
        // Arrange
        var error = new Error("NOT_FOUND", "Resource not found");

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 404);

        // Assert
        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions["errorCode"].Should().Be("NOT_FOUND");
    }

    [Fact]
    public void CreateFromError_WithIncludeErrorCodeFalse_ShouldNotIncludeErrorCode()
    {
        // Arrange
        var error = new Error("NOT_FOUND", "Resource not found");
        var options = new VerdictProblemDetailsOptions { IncludeErrorCode = false };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 404, options);

        // Assert
        problemDetails.Extensions.Should().NotContainKey("errorCode");
    }

    [Fact]
    public void CreateFromError_WithExceptionAndIncludeExceptionDetailsFalse_ShouldNotIncludeExceptionType()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = Error.FromException(exception, sanitize: false);
        var options = new VerdictProblemDetailsOptions { IncludeExceptionDetails = false };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Extensions.Should().NotContainKey("exceptionType");
        problemDetails.Extensions.Should().NotContainKey("stackTrace");
    }

    [Fact]
    public void CreateFromError_WithExceptionAndIncludeExceptionDetailsTrue_ShouldIncludeExceptionType()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = Error.FromException(exception, sanitize: false);
        var options = new VerdictProblemDetailsOptions { IncludeExceptionDetails = true };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Extensions.Should().ContainKey("exceptionType");
        problemDetails.Extensions["exceptionType"].Should().Be("InvalidOperationException");
    }

    [Fact]
    public void CreateFromError_WithIncludeStackTraceTrue_ShouldIncludeStackTrace()
    {
        // Arrange
        Exception exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        var error = Error.FromException(exception, sanitize: false);
        var options = new VerdictProblemDetailsOptions 
        { 
            IncludeExceptionDetails = true, 
            IncludeStackTrace = true 
        };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Extensions.Should().ContainKey("stackTrace");
    }

    [Fact]
    public void CreateFromError_ServerError_WithIncludeErrorMessageFalse_ShouldUseGenericMessage()
    {
        // Arrange
        var error = new Error("DB_ERROR", "Connection string: server=prod;password=secret");
        var options = new VerdictProblemDetailsOptions { IncludeErrorMessage = false };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Detail.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void CreateFromError_ServerError_WithCustomGenericMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var error = new Error("DB_ERROR", "Sensitive info");
        var options = new VerdictProblemDetailsOptions 
        { 
            IncludeErrorMessage = false,
            GenericErrorMessage = "A server error occurred. Please try again later."
        };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Detail.Should().Be("A server error occurred. Please try again later.");
    }

    [Fact]
    public void CreateFromError_ClientError_WithIncludeErrorMessageFalse_ShouldSuppressMessage()
    {
        // Changed in 3.0. Suppression used to key on statusCode >= 500, so a 4xx
        // kept its message whatever this option said. An error built from an
        // exception maps to 400 unless someone maps it, so the option could never
        // suppress the messages it existed for.
        var error = new Error("VALIDATION_ERROR", "Email is required");
        var options = new VerdictProblemDetailsOptions { IncludeErrorMessage = false };

        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 400, options);

        problemDetails.Detail.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void GenericServerErrorMessage_StillForwardsToTheNewName()
    {
        // Renamed in 3.0 because it now applies to any suppressed message rather
        // than only 5xx. The old name keeps working so an upgrade is not a
        // compile break, and this is what says so.
        var options = new VerdictProblemDetailsOptions();

#pragma warning disable CS0618
        options.GenericServerErrorMessage = "hidden";

        options.GenericErrorMessage.Should().Be("hidden");
        options.GenericServerErrorMessage.Should().Be("hidden");
#pragma warning restore CS0618
    }

    [Fact]
    public void VerdictProblemDetailsOptions_DefaultValues_ShouldBeSecure()
    {
        // Arrange & Act
        var options = new VerdictProblemDetailsOptions();

        // Assert - Secure defaults
        options.IncludeExceptionDetails.Should().BeFalse();
        options.IncludeStackTrace.Should().BeFalse();
        options.IncludeErrorCode.Should().BeTrue();
        options.IncludeErrorMessage.Should().BeTrue();
        options.GenericErrorMessage.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void SetDefaultOptions_ShouldAffectSubsequentCalls()
    {
        // Arrange
        var customOptions = new VerdictProblemDetailsOptions
        {
            IncludeErrorCode = false,
            GenericErrorMessage = "Custom error"
        };

        // Act
        ProblemDetailsFactory.SetDefaultOptions(customOptions);
        var error = new Error("TEST", "Test message");
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 400);

        // Assert
        problemDetails.Extensions.Should().NotContainKey("errorCode");

        // No inline cleanup: Dispose restores the static whether or not this assertion holds.
    }
}
