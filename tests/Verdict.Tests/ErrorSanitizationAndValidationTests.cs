using System;
using FluentAssertions;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Tests for Error sanitization and validation features.
/// </summary>
public class ErrorSanitizationAndValidationTests
{
    #region Exception Sanitization Tests

    [Fact]
    public void FromException_WithSanitizeTrue_ShouldUseDefaultSanitizedMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Sensitive database connection string: server=prod;password=secret");

        // Act
        var error = Error.FromException(exception, sanitize: true);

        // Assert
        error.Code.Should().Be("InvalidOperationException");
        error.Message.Should().Be("An error occurred.");
        error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void FromException_WithSanitizeTrueAndCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Sensitive info");

        // Act
        var error = Error.FromException(exception, sanitize: true, sanitizedMessage: "A database error occurred.");

        // Assert
        error.Message.Should().Be("A database error occurred.");
        error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void FromException_WithSanitizeFalse_ShouldPreserveOriginalMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Original message");

        // Act
        var error = Error.FromException(exception, sanitize: false);

        // Assert
        error.Message.Should().Be("Original message");
    }

    [Fact]
    public void FromException_WithCustomCode_ShouldUseCustomCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Error message");

        // Act
        var error = Error.FromException(exception, "DATABASE_ERROR");

        // Assert
        error.Code.Should().Be("DATABASE_ERROR");
        error.Message.Should().Be("Error message");
    }

    [Fact]
    public void FromException_WithCustomCodeAndSanitize_ShouldSanitizeMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Sensitive info");

        // Act
        var error = Error.FromException(exception, "DB_ERR", sanitize: true, sanitizedMessage: "Database unavailable");

        // Assert
        error.Code.Should().Be("DB_ERR");
        error.Message.Should().Be("Database unavailable");
    }

    [Fact]
    public void FromException_WithNullException_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => Error.FromException(null!, sanitize: true);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Error Code Validation Tests

    [Theory]
    [InlineData("VALID_CODE")]
    [InlineData("ValidCode123")]
    [InlineData("A")]
    [InlineData("ABC_DEF_123")]
    [InlineData("error_code")]
    public void ValidateErrorCode_WithValidCode_ShouldNotThrow(string code)
    {
        // Act
        Action act = () => Error.ValidateErrorCode(code);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("INVALID-CODE")]
    [InlineData("invalid.code")]
    [InlineData("code with spaces")]
    [InlineData("code@special")]
    [InlineData("code#tag")]
    public void ValidateErrorCode_WithInvalidCode_ShouldThrowArgumentException(string code)
    {
        // Act
        Action act = () => Error.ValidateErrorCode(code);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contains invalid character*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateErrorCode_WithNullOrEmpty_ShouldThrowArgumentNullException(string? code)
    {
        // Act
        Action act = () => Error.ValidateErrorCode(code!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("VALID_CODE", true)]
    [InlineData("ValidCode123", true)]
    [InlineData("INVALID-CODE", false)]
    [InlineData("code.with.dots", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidErrorCode_ShouldReturnCorrectResult(string? code, bool expected)
    {
        // Act
        var result = Error.IsValidErrorCode(code!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CreateValidated_WithValidCode_ShouldCreateError()
    {
        // Act
        var error = Error.CreateValidated("NOT_FOUND", "Resource not found");

        // Assert
        error.Code.Should().Be("NOT_FOUND");
        error.Message.Should().Be("Resource not found");
    }

    [Fact]
    public void CreateValidated_WithInvalidCode_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Error.CreateValidated("invalid-code", "Message");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateValidated_WithException_ShouldCreateErrorWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");

        // Act
        var error = Error.CreateValidated("ERROR_CODE", "Message", exception);

        // Assert
        error.Code.Should().Be("ERROR_CODE");
        error.Exception.Should().BeSameAs(exception);
    }

    #endregion
}
