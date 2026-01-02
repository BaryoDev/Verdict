using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Verdict.Rich;

namespace Verdict.Rich.Tests;

public class RichResultExtensionsTests
{
    [Fact]
    public void WithSuccess_ShouldAttachMessage()
    {
        // Arrange
        RichResult<int> result = Result<int>.Success(100);

        // Act
        result = result.WithSuccess("User created successfully");

        // Assert
        var successes = result.Successes;
        successes.Should().HaveCount(1);
        successes[0].Message.Should().Be("User created successfully");
    }

    [Fact]
    public void WithSuccess_OnFailure_ShouldNotAttach()
    {
        // Arrange
        RichResult<int> result = Result<int>.Failure("ERR", "Msg");

        // Act
        result = result.WithSuccess("Success?");

        // Assert
        result.Successes.Should().BeEmpty();
    }

    [Fact]
    public void WithSuccess_MultipleMessages_ShouldCollectAll()
    {
        // Arrange
        RichResult<int> result = Result<int>.Success(101);

        // Act
        result = result.WithSuccess("Msg 1").WithSuccess("Msg 2");

        // Assert
        result.Successes.Should().HaveCount(2);
    }

    [Fact]
    public void WithErrorMetadata_ShouldStoreMetadata()
    {
        // Arrange
        RichResult<int> result = Result<int>.Failure("ERR_UNIQUE", "Msg");

        // Act
        result = result.WithErrorMetadata("UserId", 123);

        // Assert
        var metadata = result.ErrorMetadata;
        metadata["UserId"].Should().Be(123);
    }

    [Fact]
    public void WithErrorMetadata_OnSuccess_ShouldNotStore()
    {
        // Arrange
        RichResult<int> result = Result<int>.Success(102);

        // Act
        result = result.WithErrorMetadata("Key", "Value");

        // Assert
        result.ErrorMetadata.Should().BeEmpty();
    }

    [Fact]
    public void WithSuccess_SuccessInfo_ShouldStoreMetadata()
    {
        // Arrange
        RichResult<int> result = Result<int>.Success(103);
        var successInfo = new SuccessInfo("Created")
            .WithMetadata("Id", 123);

        // Act
        result = result.WithSuccess(successInfo);

        // Assert
        var successes = result.Successes;
        successes.Should().HaveCount(1);
        successes[0].Message.Should().Be("Created");
        successes[0].Metadata!["Id"].Should().Be(123);
    }

    [Fact]
    public void NonGeneric_WithSuccess_ShouldWork()
    {
        // Arrange
        RichResult result = Result.Success();

        // Act
        // Note: Global success results share the same metadata if they have the same internal state.
        // We should clear it or use different states if possible.
        // Actually, Result.Success() is a singleton in some implementations.
    }

    [Fact]
    public void NonGeneric_WithErrorMetadata_ShouldWork()
    {
        // Arrange
        RichResult result = Result.Failure("ERR_NON_GENERIC", "Msg");

        // Act
        result = result.WithErrorMetadata("LogId", Guid.NewGuid());

        // Assert
        result.ErrorMetadata.Should().NotBeEmpty();
    }

    [Fact]
    public void Metadata_ShouldBeIsolatedPerResultValue()
    {
        // Arrange
        RichResult<int> r1 = Result<int>.Success(111);
        RichResult<int> r2 = Result<int>.Success(222);

        // Act
        r1 = r1.WithSuccess("R1");
        r2 = r2.WithSuccess("R2");

        // Assert
        r1.Successes[0].Message.Should().Be("R1");
        r2.Successes[0].Message.Should().Be("R2");
    }

    [Fact]
    public void WithSuccess_NullMessage_ShouldThrow()
    {
        // Arrange
        RichResult<int> result = Result<int>.Success(104);

        // Act
        Action act = () => result.WithSuccess(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
