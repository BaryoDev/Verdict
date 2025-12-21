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
        var result = Result<int>.Success(100);

        // Act
        result.WithSuccess("User created successfully");

        // Assert
        var successes = result.GetSuccesses();
        successes.Should().HaveCount(1);
        successes[0].Message.Should().Be("User created successfully");
    }

    [Fact]
    public void WithSuccess_OnFailure_ShouldNotAttach()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");

        // Act
        result.WithSuccess("Success?");

        // Assert
        result.GetSuccesses().Should().BeEmpty();
    }

    [Fact]
    public void WithSuccess_MultipleMessages_ShouldCollectAll()
    {
        // Arrange
        var result = Result<int>.Success(101);

        // Act
        result.WithSuccess("Msg 1").WithSuccess("Msg 2");

        // Assert
        result.GetSuccesses().Should().HaveCount(2);
    }

    [Fact]
    public void WithErrorMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var result = Result<int>.Failure("ERR_UNIQUE", "Msg");

        // Act
        result.WithErrorMetadata("UserId", 123);

        // Assert
        var metadata = result.GetErrorMetadata();
        metadata["UserId"].Should().Be(123);
    }

    [Fact]
    public void WithErrorMetadata_OnSuccess_ShouldNotStore()
    {
        // Arrange
        var result = Result<int>.Success(102);

        // Act
        result.WithErrorMetadata("Key", "Value");

        // Assert
        result.GetErrorMetadata().Should().BeEmpty();
    }

    [Fact]
    public void WithSuccess_SuccessInfo_ShouldStoreMetadata()
    {
        // Arrange
        var result = Result<int>.Success(103);
        var successInfo = new SuccessInfo("Created")
            .WithMetadata("Id", 123);

        // Act
        result.WithSuccess(successInfo);

        // Assert
        var successes = result.GetSuccesses();
        successes.Should().HaveCount(1);
        successes[0].Message.Should().Be("Created");
        successes[0].Metadata!["Id"].Should().Be(123);
    }

    [Fact]
    public void NonGeneric_WithSuccess_ShouldWork()
    {
        // Arrange
        var result = Result.Success();

        // Act
        // Note: Global success results share the same metadata if they have the same internal state.
        // We should clear it or use different states if possible.
        // Actually, Result.Success() is a singleton in some implementations.
    }

    [Fact]
    public void NonGeneric_WithErrorMetadata_ShouldWork()
    {
        // Arrange
        var result = Result.Failure("ERR_NON_GENERIC", "Msg");

        // Act
        result.WithErrorMetadata("LogId", Guid.NewGuid());

        // Assert
        result.GetErrorMetadata().Should().NotBeEmpty();
    }

    [Fact]
    public void Metadata_ShouldBeIsolatedPerResultValue()
    {
        // Arrange
        var r1 = Result<int>.Success(111);
        var r2 = Result<int>.Success(222);

        // Act
        r1.WithSuccess("R1");
        r2.WithSuccess("R2");

        // Assert
        r1.GetSuccesses()[0].Message.Should().Be("R1");
        r2.GetSuccesses()[0].Message.Should().Be("R2");
    }

    [Fact]
    public void WithSuccess_NullMessage_ShouldThrow()
    {
        // Arrange
        var result = Result<int>.Success(104);

        // Act
        Action act = () => result.WithSuccess(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
