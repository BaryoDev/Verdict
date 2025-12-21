using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Verdict.AspNetCore;

/// <summary>
/// Factory for creating RFC 7807 ProblemDetails from Error.
/// </summary>
public static class ProblemDetailsFactory
{
    /// <summary>
    /// Creates ProblemDetails from an Error.
    /// Maps Error.Code to ProblemDetails.Type
    /// Maps Error.Message to ProblemDetails.Detail
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <param name="statusCode">HTTP status code (default: 400).</param>
    /// <returns>RFC 7807 compliant ProblemDetails.</returns>
    public static ProblemDetails CreateFromError(Error error, int statusCode = 400)
    {
        var problemDetails = new ProblemDetails
        {
            Type = GetProblemType(statusCode),
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = error.Message
        };

        // Add error code as extension
        problemDetails.Extensions["errorCode"] = error.Code;

        // Add exception details if present (only in development)
        if (error.Exception != null)
        {
            problemDetails.Extensions["exceptionType"] = error.Exception.GetType().Name;
        }

        return problemDetails;
    }

    /// <summary>
    /// Creates ValidationProblemDetails from MultiResult errors.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The multi-result with errors.</param>
    /// <returns>RFC 7807 compliant ValidationProblemDetails.</returns>
    public static ValidationProblemDetails CreateFromMultiResult<T>(Verdict.Extensions.MultiResult<T> result)
    {
        var errors = new Dictionary<string, string[]>();
        
        var errorList = new List<string>();
        foreach (var error in result.Errors)
        {
            errorList.Add($"[{error.Code}] {error.Message}");
        }

        errors["errors"] = errorList.ToArray();

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = 400,
            Detail = $"{result.ErrorCount} validation error(s) occurred."
        };

        return problemDetails;
    }

    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => "https://tools.ietf.org/html/rfc7231"
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            _ => "Error"
        };
    }
}
