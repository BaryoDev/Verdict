using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Verdict;
using Verdict.AspNetCore;
using Verdict.Extensions;
using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// Production readiness tests for ASP.NET Core integration.
/// Tests thread-safety, concurrent access, and edge cases.
/// </summary>
public class ProductionReadinessTests
{
    #region ErrorStatusCodeMapper Thread Safety

    [Fact]
    public async Task ErrorStatusCodeMapper_ConcurrentRegistration_ShouldBeSafe()
    {
        // Use unique prefix to avoid test pollution
        var prefix = $"CONC_{Guid.NewGuid():N}_";

        // Arrange & Act
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            ErrorStatusCodeMapper.RegisterMapping($"{prefix}{i}", 400 + (i % 100));
            return ErrorStatusCodeMapper.GetStatusCode(new Error($"{prefix}{i}", "msg"));
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - all registrations should succeed
        results.Should().AllSatisfy(code => code.Should().BeInRange(400, 499));

        // Cleanup
        for (int i = 0; i < 100; i++)
        {
            ErrorStatusCodeMapper.RemoveMapping($"{prefix}{i}");
        }
    }

    [Fact]
    public async Task ErrorStatusCodeMapper_ConcurrentReads_ShouldBeSafe()
    {
        // Arrange - use unique code to avoid test pollution
        var uniqueCode = $"CONC_READ_{Guid.NewGuid():N}";
        ErrorStatusCodeMapper.RegisterMapping(uniqueCode, 404);
        var error = new Error(uniqueCode, "Test");

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
            ErrorStatusCodeMapper.GetStatusCode(error)
        )).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(code => code.Should().Be(404));

        // Cleanup
        ErrorStatusCodeMapper.RemoveMapping(uniqueCode);
    }

    [Fact]
    public async Task ErrorStatusCodeMapper_ConcurrentReadAndWrite_ShouldBeSafe()
    {
        // Arrange
        var uniqueCode = $"RW_TEST_{Guid.NewGuid():N}";
        var error = new Error(uniqueCode, "Test");

        // Act - concurrent reads and writes
        var readTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            ErrorStatusCodeMapper.GetStatusCode(error)
        )).ToArray();

        var writeTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            ErrorStatusCodeMapper.RegisterMapping(uniqueCode, 400 + i);
            return true;
        })).ToArray();

        await Task.WhenAll(readTasks);
        await Task.WhenAll(writeTasks);

        // Assert - final value should be consistent
        var finalCode = ErrorStatusCodeMapper.GetStatusCode(error);
        finalCode.Should().BeInRange(400, 449);

        // Cleanup
        ErrorStatusCodeMapper.RemoveMapping(uniqueCode);
    }

    #endregion

    #region ProblemDetailsFactory Thread Safety

    [Fact]
    public async Task ProblemDetailsFactory_ConcurrentCreation_ShouldBeSafe()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test message");

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
            ProblemDetailsFactory.CreateFromError(error, 400)
        )).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(pd =>
        {
            pd.Status.Should().Be(400);
            pd.Detail.Should().Be("Test message");
        });
    }

    [Fact]
    public async Task ProblemDetailsFactory_SetDefaultOptions_ConcurrentAccess_ShouldBeSafe()
    {
        // Arrange
        var options1 = new VerdictProblemDetailsOptions { IncludeErrorCode = true };
        var options2 = new VerdictProblemDetailsOptions { IncludeErrorCode = false };

        // Act - concurrent option changes and reads
        var writeTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            ProblemDetailsFactory.SetDefaultOptions(i % 2 == 0 ? options1 : options2);
        })).ToArray();

        var readTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var error = new Error("CODE", "Message");
            return ProblemDetailsFactory.CreateFromError(error, 400);
        })).ToArray();

        await Task.WhenAll(writeTasks);
        var results = await Task.WhenAll(readTasks);

        // Assert - no exceptions, all valid ProblemDetails
        results.Should().AllSatisfy(pd =>
        {
            pd.Should().NotBeNull();
            pd.Status.Should().Be(400);
        });
    }

    #endregion

    #region ProblemDetailsFactory Edge Cases

    [Fact]
    public void ProblemDetailsFactory_WithServerError_ShouldSanitizeMessage()
    {
        // Arrange
        var options = new VerdictProblemDetailsOptions
        {
            IncludeErrorMessage = false,
            GenericServerErrorMessage = "Internal server error"
        };
        var error = new Error("DB_ERROR", "Connection string: server=prod;password=secret");

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Detail.Should().Be("Internal server error");
        problemDetails.Detail.Should().NotContain("password");
    }

    [Fact]
    public void ProblemDetailsFactory_WithClientError_ShouldIncludeMessage()
    {
        // Arrange
        var options = new VerdictProblemDetailsOptions
        {
            IncludeErrorMessage = false // But client errors should still show
        };
        var error = new Error("VALIDATION", "Email is invalid");

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 400, options);

        // Assert
        problemDetails.Detail.Should().Be("Email is invalid");
    }

    [Fact]
    public void ProblemDetailsFactory_WithNullOptions_ShouldUseDefaults()
    {
        // Arrange
        var error = new Error("TEST", "Test message");

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 400, null!);

        // Assert
        problemDetails.Should().NotBeNull();
        problemDetails.Detail.Should().Be("Test message");
    }

    [Fact]
    public void ProblemDetailsFactory_SetDefaultOptions_WithNull_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => ProblemDetailsFactory.SetDefaultOptions(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ProblemDetailsFactory_CreateFromMultiResult_ShouldFormatErrors()
    {
        // Arrange
        var errors = new[]
        {
            new Error("E1", "Error 1"),
            new Error("E2", "Error 2"),
            new Error("E3", "Error 3")
        };
        var multiResult = MultiResult<int>.Failure(errors);

        // Act
        var validationProblemDetails = ProblemDetailsFactory.CreateFromMultiResult(multiResult);

        // Assert
        validationProblemDetails.Status.Should().Be(400);
        validationProblemDetails.Title.Should().Be("One or more validation errors occurred.");
        validationProblemDetails.Errors["errors"].Should().HaveCount(3);
    }

    [Fact]
    public void ProblemDetailsFactory_WithExceptionDetails_ShouldIncludeWhenEnabled()
    {
        // Arrange
        var options = new VerdictProblemDetailsOptions
        {
            IncludeExceptionDetails = true,
            IncludeStackTrace = true
        };
        var exception = new InvalidOperationException("Test");
        var error = new Error("ERROR", "Message", exception);

        // Act
        var problemDetails = ProblemDetailsFactory.CreateFromError(error, 500, options);

        // Assert
        problemDetails.Extensions.Should().ContainKey("exceptionType");
        problemDetails.Extensions["exceptionType"].Should().Be("InvalidOperationException");
    }

    #endregion

    #region ErrorStatusCodeMapper Edge Cases

    [Fact]
    public void ErrorStatusCodeMapper_UnknownErrorCode_ShouldReturnDefault()
    {
        // Arrange
        var error = new Error("UNKNOWN_ERROR_CODE_XYZ", "Unknown");

        // Act
        var statusCode = ErrorStatusCodeMapper.GetStatusCode(error);

        // Assert
        statusCode.Should().Be(400); // Default for unknown errors
    }

    [Fact]
    public void ErrorStatusCodeMapper_WithEmptyCode_ShouldReturnDefault()
    {
        // Arrange
        var error = new Error("", "Empty code");

        // Act
        var statusCode = ErrorStatusCodeMapper.GetStatusCode(error);

        // Assert
        statusCode.Should().Be(400);
    }

    [Fact]
    public void ErrorStatusCodeMapper_RegisterTwice_ShouldOverwrite()
    {
        // Arrange
        var uniqueCode = $"OVERWRITE_{Guid.NewGuid():N}";
        ErrorStatusCodeMapper.RegisterMapping(uniqueCode, 404);
        var error = new Error(uniqueCode, "Test");

        // Act
        ErrorStatusCodeMapper.RegisterMapping(uniqueCode, 409);

        // Assert
        ErrorStatusCodeMapper.GetStatusCode(error).Should().Be(409);

        // Cleanup
        ErrorStatusCodeMapper.RemoveMapping(uniqueCode);
    }

    #endregion

    #region ResultExtensions Tests

    [Fact]
    public void ToActionResult_Success_ShouldReturnOkWithValue()
    {
        // Arrange
        var result = Result<string>.Success("test data");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)actionResult.Result!).Value.Should().Be("test data");
    }

    [Fact]
    public void ToActionResult_Failure_ShouldReturnProblemDetails()
    {
        // Arrange
        var result = Result<string>.Failure("NOT_FOUND", "Resource not found");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Result.Should().BeOfType<ObjectResult>();
    }

    [Fact]
    public void ToActionResult_NonGeneric_Success_ShouldReturnNoContent()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    #endregion
}
