using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;
using Verdict.AspNetCore;

namespace Verdict.AspNetCore.Tests;

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
    public void GetCustomMappings_ShouldReturnRegisteredMappings()
    {
        // Arrange - use unique codes to avoid test pollution from parallel tests
        var uniquePrefix = $"UNIT_{Guid.NewGuid():N}_";
        var code1 = $"{uniquePrefix}M1";
        var code2 = $"{uniquePrefix}M2";

        ErrorStatusCodeMapper.RegisterMapping(code1, 401);
        ErrorStatusCodeMapper.RegisterMapping(code2, 402);

        try
        {
            // Act
            var mappings = ErrorStatusCodeMapper.GetCustomMappings();

            // Assert - verify our specific codes are present with correct values
            mappings.Should().ContainKey(code1);
            mappings.Should().ContainKey(code2);
            mappings[code1].Should().Be(401);
            mappings[code2].Should().Be(402);
        }
        finally
        {
            // Cleanup
            ErrorStatusCodeMapper.RemoveMapping(code1);
            ErrorStatusCodeMapper.RemoveMapping(code2);
        }
    }
}
