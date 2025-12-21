using System;
using FluentAssertions;
using Xunit;

namespace Upshot.Tests;

/// <summary>
/// Tests for Error type.
/// </summary>
public class ErrorTests
{
    [Fact]
    public void Error_WithCodeAndMessage_ShouldStoreValues()
    {
        // Arrange & Act
        var error = new Error("TEST_CODE", "Test message");

        // Assert
        error.Code.Should().Be("TEST_CODE");
        error.Message.Should().Be("Test message");
        error.Exception.Should().BeNull();
    }

    [Fact]
    public void Error_WithNullCode_ShouldDefaultToEmpty()
    {
        // Arrange & Act
        var error = new Error(null!, "Test message");

        // Assert
        error.Code.Should().BeEmpty();
    }

    [Fact]
    public void Error_WithNullMessage_ShouldDefaultToEmpty()
    {
        // Arrange & Act
        var error = new Error("TEST", null!);

        // Assert
        error.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_WithException_ShouldStoreException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");

        // Act
        var error = new Error("TEST", "Message", exception);

        // Assert
        error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Error_Create_ShouldCreateError()
    {
        // Arrange & Act
        var error = Error.Create("CODE", "Message");

        // Assert
        error.Code.Should().Be("CODE");
        error.Message.Should().Be("Message");
    }

    [Fact]
    public void Error_CreateWithException_ShouldCreateError()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var error = Error.Create("CODE", "Message", exception);

        // Assert
        error.Code.Should().Be("CODE");
        error.Message.Should().Be("Message");
        error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Error_FromException_ShouldExtractTypeAndMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var error = Error.FromException(exception);

        // Assert
        error.Code.Should().Be("InvalidOperationException");
        error.Message.Should().Be("Test exception");
        error.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Error_WithException_ShouldAttachException()
    {
        // Arrange
        var error = new Error("CODE", "Message");
        var exception = new Exception("Test");

        // Act
        var errorWithException = error.WithException(exception);

        // Assert
        errorWithException.Code.Should().Be("CODE");
        errorWithException.Message.Should().Be("Message");
        errorWithException.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Error_Equality_ShouldWorkAsRecordStruct()
    {
        // Arrange
        var error1 = new Error("CODE", "Message");
        var error2 = new Error("CODE", "Message");
        var error3 = new Error("OTHER", "Message");

        // Act & Assert
        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
    }

    [Fact]
    public void Error_ToString_ShouldContainCodeAndMessage()
    {
        // Arrange
        var error = new Error("TEST_CODE", "Test message");

        // Act
        var stringValue = error.ToString();

        // Assert
        stringValue.Should().Contain("TEST_CODE");
        stringValue.Should().Contain("Test message");
    }
}
