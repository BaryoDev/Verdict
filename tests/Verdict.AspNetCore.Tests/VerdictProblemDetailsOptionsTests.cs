using System;
using FluentAssertions;
using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// Tests for VerdictProblemDetailsOptions configuration.
/// </summary>
public class VerdictProblemDetailsOptionsTests
{
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
        var error = Error.FromException(exception);
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
        var error = Error.FromException(exception);
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
        var error = Error.FromException(exception);
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
            GenericServerErrorMessage = "A server error occurred. Please try again later."
        };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Detail.Should().Be("A server error occurred. Please try again later.");
    }

    [Fact]
    public void CreateFromError_ClientError_WithIncludeErrorMessageFalse_ShouldStillIncludeMessage()
    {
        // Arrange - Client errors (4xx) should still show the message even when IncludeErrorMessage is false
        var error = new Error("VALIDATION_ERROR", "Email is required");
        var options = new VerdictProblemDetailsOptions { IncludeErrorMessage = false };

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 400, options);

        // Assert
        problemDetails.Detail.Should().Be("Email is required");
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
        options.GenericServerErrorMessage.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void SetDefaultOptions_ShouldAffectSubsequentCalls()
    {
        // Arrange
        var customOptions = new VerdictProblemDetailsOptions
        {
            IncludeErrorCode = false,
            GenericServerErrorMessage = "Custom error"
        };

        // Act
        ProblemDetailsFactory.SetDefaultOptions(customOptions);
        var error = new Error("TEST", "Test message");
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 400);

        // Assert
        problemDetails.Extensions.Should().NotContainKey("errorCode");

        // Cleanup - Reset to default
        ProblemDetailsFactory.SetDefaultOptions(new VerdictProblemDetailsOptions());
    }
}
