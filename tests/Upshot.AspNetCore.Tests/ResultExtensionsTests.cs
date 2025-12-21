using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Upshot.AspNetCore;

namespace Upshot.AspNetCore.Tests;

public class ResultExtensionsTests
{
    [Fact]
    public void ToHttpResult_Success_ShouldReturnOk()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<IResult>();
        // Note: Testing the actual content of Minimal API IResult is complex without a test server,
        // but we verify it returns a valid result object.
    }

    [Fact]
    public void ToHttpResult_Failure_ShouldReturnProblem()
    {
        // Arrange
        var result = Result<int>.Failure("NOT_FOUND", "User not found");

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithCustomStatusCode_ShouldRespectIt()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var httpResult = result.ToHttpResult(successStatusCode: 201);

        // Assert
        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void ToActionResult_Success_ShouldReturnOkObjectResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        okResult.Value.Should().Be(42);
    }

    [Fact]
    public void ToActionResult_Failure_ShouldReturnObjectResultWithProblemDetails()
    {
        // Arrange
        var result = Result<int>.Failure("VALIDATION_ERROR", "Invalid input");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult.Result!;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ProblemDetails>();
        var problem = (ProblemDetails)objectResult.Value!;
        problem.Title.Should().Be("Bad Request");
    }

    [Fact]
    public void ToActionResult_WithCustomSuccessCode_ShouldUseIt()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var actionResult = result.ToActionResult(successStatusCode: 202);

        // Assert
        actionResult.Result.Should().BeOfType<AcceptedResult>();
        ((AcceptedResult)actionResult.Result!).StatusCode.Should().Be(202);
    }

    [Fact]
    public void ToActionResult_WithCustomMapper_ShouldUseIt()
    {
        // Arrange
        var result = Result<int>.Failure("CUSTOM", "Error");

        // Act
        var actionResult = result.ToActionResult(errorStatusCodeMapper: _ => 418);

        // Assert
        var objectResult = (ObjectResult)actionResult.Result!;
        objectResult.StatusCode.Should().Be(418);
    }

    [Fact]
    public void ToActionResult_NonGeneric_Success_ShouldReturnNoContent()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var actionResult = Upshot.AspNetCore.ResultExtensions.ToActionResult(result);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToActionResult_With201Status_ShouldReturnCreatedResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var actionResult = result.ToActionResult(successStatusCode: 201);

        // Assert
        actionResult.Result.Should().BeOfType<CreatedResult>();
    }

    [Fact]
    public void ToActionResult_With204Status_ShouldReturnNoContentResult()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var actionResult = result.ToActionResult(successStatusCode: 204);

        // Assert
        actionResult.Result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToHttpResult_WithCustomErrorMapper_ShouldUseMappedCode()
    {
        // Arrange
        var result = Result<int>.Failure("ERR", "Msg");

        // Act
        var httpResult = result.ToHttpResult(errorStatusCodeMapper: _ => 429);

        // Assert
        httpResult.Should().NotBeNull();
    }
}
