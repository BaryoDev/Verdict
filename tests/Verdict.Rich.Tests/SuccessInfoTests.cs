using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Verdict;
using Verdict.Rich;
using Xunit;

namespace Verdict.Rich.Tests;

/// <summary>
/// Tests for SuccessInfo struct and CustomErrorExtensions.
/// </summary>
public class SuccessInfoTests
{
    #region SuccessInfo Basic Tests

    [Fact]
    public void SuccessInfo_Constructor_ShouldSetMessage()
    {
        // Arrange & Act
        var info = new SuccessInfo("Operation completed");

        // Assert
        info.Message.Should().Be("Operation completed");
        info.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void SuccessInfo_Constructor_WithNullMessage_ShouldUseEmptyString()
    {
        // Arrange & Act
        var info = new SuccessInfo(null!);

        // Assert
        info.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void SuccessInfo_Default_ShouldHaveEmptyMetadata()
    {
        // Arrange & Act
        var info = default(SuccessInfo);

        // Assert
        info.Message.Should().BeNull();
        info.Metadata.Should().BeEmpty();
    }

    #endregion

    #region WithMetadata Tests

    [Fact]
    public void SuccessInfo_WithMetadata_ShouldAddMetadata()
    {
        // Arrange
        var info = new SuccessInfo("Operation completed");

        // Act
        var withMeta = info.WithMetadata("Duration", 100);

        // Assert
        withMeta.Metadata.Should().ContainKey("Duration");
        withMeta.Metadata["Duration"].Should().Be(100);
        withMeta.Message.Should().Be("Operation completed");
    }

    [Fact]
    public void SuccessInfo_WithMetadata_ShouldBeImmutable()
    {
        // Arrange
        var original = new SuccessInfo("Original");

        // Act
        var withMeta = original.WithMetadata("Key", "Value");

        // Assert - original unchanged
        original.Metadata.Should().BeEmpty();
        withMeta.Metadata.Should().HaveCount(1);
    }

    [Fact]
    public void SuccessInfo_WithMetadata_MultipleAdditions_ShouldAccumulate()
    {
        // Arrange
        var info = new SuccessInfo("Operation");

        // Act
        var withMeta = info
            .WithMetadata("Key1", "Value1")
            .WithMetadata("Key2", 42)
            .WithMetadata("Key3", true);

        // Assert
        withMeta.Metadata.Should().HaveCount(3);
        withMeta.Metadata["Key1"].Should().Be("Value1");
        withMeta.Metadata["Key2"].Should().Be(42);
        withMeta.Metadata["Key3"].Should().Be(true);
    }

    [Fact]
    public void SuccessInfo_WithMetadata_SameKey_ShouldOverwrite()
    {
        // Arrange
        var info = new SuccessInfo("Operation")
            .WithMetadata("Key", "Original");

        // Act
        var updated = info.WithMetadata("Key", "Updated");

        // Assert
        updated.Metadata["Key"].Should().Be("Updated");
        updated.Metadata.Should().HaveCount(1);
    }

    [Fact]
    public void SuccessInfo_WithMetadata_NullKey_ShouldThrow()
    {
        // Arrange
        var info = new SuccessInfo("Operation");

        // Act
        Action act = () => info.WithMetadata(null!, "value");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SuccessInfo_WithMetadata_NullValue_ShouldThrow()
    {
        // Arrange
        var info = new SuccessInfo("Operation");

        // Act
        Action act = () => info.WithMetadata("key", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void SuccessInfo_ToString_WithoutMetadata_ShouldReturnMessage()
    {
        // Arrange
        var info = new SuccessInfo("Operation completed");

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Be("Operation completed");
    }

    [Fact]
    public void SuccessInfo_ToString_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        var info = new SuccessInfo("Operation")
            .WithMetadata("Duration", 100);

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Contain("Operation");
        result.Should().Contain("Duration=100");
    }

    [Fact]
    public void SuccessInfo_ToString_WithMultipleMetadata_ShouldFormat()
    {
        // Arrange
        var info = new SuccessInfo("Done")
            .WithMetadata("A", 1)
            .WithMetadata("B", 2);

        // Act
        var result = info.ToString();

        // Assert
        result.Should().StartWith("Done (");
        result.Should().Contain("A=1");
        result.Should().Contain("B=2");
        result.Should().EndWith(")");
    }

    #endregion

    #region Large Metadata Tests

    [Fact]
    public void SuccessInfo_WithManyMetadataEntries_ShouldHandle()
    {
        // Arrange
        var info = new SuccessInfo("Operation");

        // Act - add 100 metadata entries
        for (int i = 0; i < 100; i++)
        {
            info = info.WithMetadata($"Key{i}", i);
        }

        // Assert
        info.Metadata.Should().HaveCount(100);
        info.Metadata["Key50"].Should().Be(50);
    }

    [Fact]
    public void SuccessInfo_WithLargeValues_ShouldHandle()
    {
        // Arrange
        var largeString = new string('x', 10_000);
        var info = new SuccessInfo("Operation");

        // Act
        var withMeta = info.WithMetadata("LargeValue", largeString);

        // Assert
        withMeta.Metadata["LargeValue"].Should().Be(largeString);
    }

    #endregion
}

/// <summary>
/// Tests for CustomErrorExtensions.
/// </summary>
public class CustomErrorExtensionsTests
{
    private class TestErrorMetadata : IErrorMetadata
    {
        public string ErrorType { get; set; } = "TestError";
        public Dictionary<string, object> MetadataItems { get; set; } = new();

        public string GetErrorType() => ErrorType;
        public Dictionary<string, object> GetMetadata() => MetadataItems;
    }

    [Fact]
    public void WithCustomError_Generic_OnFailure_ShouldAttachMetadata()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test error");
        var metadata = new TestErrorMetadata
        {
            ErrorType = "ValidationError",
            MetadataItems = new Dictionary<string, object>
            {
                ["Field"] = "Email",
                ["Value"] = "invalid"
            }
        };

        // Act
        var rich = result.WithCustomError(metadata);

        // Assert
        rich.IsFailure.Should().BeTrue();
        rich.ErrorMetadata.Should().ContainKey("ErrorType");
        rich.ErrorMetadata["ErrorType"].Should().Be("ValidationError");
        rich.ErrorMetadata["Field"].Should().Be("Email");
        rich.ErrorMetadata["Value"].Should().Be("invalid");
    }

    [Fact]
    public void WithCustomError_Generic_OnSuccess_ShouldReturnUnchanged()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var metadata = new TestErrorMetadata();

        // Act
        var rich = result.WithCustomError(metadata);

        // Assert
        rich.IsSuccess.Should().BeTrue();
        rich.Value.Should().Be(42);
        rich.ErrorMetadata.Should().BeEmpty();
    }

    [Fact]
    public void WithCustomError_Generic_WithNullMetadata_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test");

        // Act
        Action act = () => result.WithCustomError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithCustomError_NonGeneric_OnFailure_ShouldAttachMetadata()
    {
        // Arrange
        var result = Result.Failure("ERROR", "Test error");
        var metadata = new TestErrorMetadata
        {
            ErrorType = "SystemError",
            MetadataItems = new Dictionary<string, object>
            {
                ["Source"] = "Database"
            }
        };

        // Act
        var rich = result.WithCustomError(metadata);

        // Assert
        rich.IsFailure.Should().BeTrue();
        rich.ErrorMetadata["ErrorType"].Should().Be("SystemError");
        rich.ErrorMetadata["Source"].Should().Be("Database");
    }

    [Fact]
    public void WithCustomError_NonGeneric_OnSuccess_ShouldReturnUnchanged()
    {
        // Arrange
        var result = Result.Success();
        var metadata = new TestErrorMetadata();

        // Act
        var rich = result.WithCustomError(metadata);

        // Assert
        rich.IsSuccess.Should().BeTrue();
        rich.ErrorMetadata.Should().BeEmpty();
    }

    [Fact]
    public void WithCustomError_NonGeneric_WithNullMetadata_ShouldThrow()
    {
        // Arrange
        var result = Result.Failure("ERROR", "Test");

        // Act
        Action act = () => result.WithCustomError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithCustomError_WithEmptyMetadata_ShouldOnlyAddErrorType()
    {
        // Arrange
        var result = Result<int>.Failure("ERROR", "Test");
        var metadata = new TestErrorMetadata
        {
            ErrorType = "EmptyError",
            MetadataItems = new Dictionary<string, object>()
        };

        // Act
        var rich = result.WithCustomError(metadata);

        // Assert
        rich.ErrorMetadata.Should().HaveCount(1);
        rich.ErrorMetadata["ErrorType"].Should().Be("EmptyError");
    }
}
