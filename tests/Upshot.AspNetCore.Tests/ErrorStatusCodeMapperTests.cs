using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;
using Upshot.AspNetCore;

namespace Upshot.AspNetCore.Tests;

public class ErrorStatusCodeMapperTests
{
    [Theory]
    [InlineData("NOT_FOUND", 404)]
    [InlineData("UNAUTHORIZED", 401)]
    [InlineData("FORBIDDEN", 403)]
    [InlineData("BAD_REQUEST", 400)]
    [InlineData("CONFLICT", 409)]
    [InlineData("VALIDATION_ERROR", 400)]
    [InlineData("INTERNAL_ERROR", 500)]
    public void GetStatusCode_CommonCodes_ShouldMapCorrectly(string code, int expected)
    {
        // Arrange
        var error = new Error(code, "Message");

        // Act
        var statusCode = ErrorStatusCodeMapper.GetStatusCode(error);

        // Assert
        statusCode.Should().Be(expected);
    }

    [Fact]
    public void GetStatusCode_UnknownCode_ShouldDefaultTo400()
    {
        // Arrange
        var error = new Error("UNKNOWN_STUFF", "Message");

        // Act
        var statusCode = ErrorStatusCodeMapper.GetStatusCode(error);

        // Assert
        statusCode.Should().Be(400);
    }

    [Fact]
    public void RegisterMapping_ShouldOverrideDefault()
    {
        // Arrange
        var code = "NEW_CODE";
        var expected = 418;

        // Act
        ErrorStatusCodeMapper.RegisterMapping(code, expected);
        var statusCode = ErrorStatusCodeMapper.GetStatusCode(new Error(code, ""));

        // Assert
        statusCode.Should().Be(expected);
    }

    [Fact]
    public void ClearCustomMappings_ShouldRemoveAllMappings()
    {
        // Arrange
        ErrorStatusCodeMapper.RegisterMapping("TEMP", 418);

        // Act
        ErrorStatusCodeMapper.ClearCustomMappings();
        var statusCode = ErrorStatusCodeMapper.GetStatusCode(new Error("TEMP", ""));

        // Assert
        statusCode.Should().Be(400); // Back to default
    }

    [Fact]
    public void GetCustomMappings_ShouldReturnAllRegisteredMappings()
    {
        // Arrange
        ErrorStatusCodeMapper.ClearCustomMappings();
        ErrorStatusCodeMapper.RegisterMapping("M1", 401);
        ErrorStatusCodeMapper.RegisterMapping("M2", 402);

        // Act
        var mappings = ErrorStatusCodeMapper.GetCustomMappings();

        // Assert
        mappings.Should().HaveCount(2);
        mappings["M1"].Should().Be(401);
        mappings["M2"].Should().Be(402);
    }
}
