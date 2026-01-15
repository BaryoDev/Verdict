using System;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Verdict.AspNetCore;
using Verdict.Extensions;

namespace Verdict.AspNetCore.Tests;

public class ProblemDetailsFactoryTests
{
    [Fact]
    public void CreateFromError_ShouldMapCorrectly()
    {
        // Arrange
        var error = new Error("TEST_CODE", "Test message");

        // Act
        var problem = ProblemDetailsFactory.CreateFromError(error, 404);

        // Assert
        problem.Status.Should().Be(404);
        problem.Title.Should().Be("Not Found");
        problem.Detail.Should().Be("Test message");
        problem.Extensions["errorCode"].Should().Be("TEST_CODE");
    }

    [Fact]
    public void CreateFromMultiResult_ShouldReturnValidationProblem()
    {
        // Arrange
        var errors = new[]
        {
            new Error("ERR1", "Msg 1"),
            new Error("ERR2", "Msg 2")
        };
        var result = MultiResult<int>.Failure(errors);

        // Act
        var problem = ProblemDetailsFactory.CreateFromMultiResult(result);

        // Assert
        problem.Status.Should().Be(400);
        ((string[])problem.Errors["errors"]).Should().HaveCount(2);
        ((string[])problem.Errors["errors"]).Should().Contain("[ERR1] Msg 1");
    }

    [Fact]
    public void CreateFromError_WithException_ShouldIncludeDetailsWhenEnabled()
    {
        // Arrange
        var ex = new InvalidOperationException("Inner error");
        var error = Error.FromException(ex);
        var options = new VerdictProblemDetailsOptions { IncludeExceptionDetails = true };

        // Act
        var problem = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problem.Extensions["exceptionType"].Should().Be("InvalidOperationException");
    }

    [Fact]
    public void CreateFromError_WithException_ShouldNotIncludeDetailsByDefault()
    {
        // Arrange
        var ex = new InvalidOperationException("Inner error");
        var error = Error.FromException(ex);

        // Act
        var problem = ProblemDetailsFactory.CreateFromError(error, 500);

        // Assert
        problem.Extensions.Should().NotContainKey("exceptionType");
    }
}
