using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Verdict.AspNetCore;

/// <summary>
/// ASP.NET Core extensions for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts Result{T} to IResult (Minimal API).
    /// Success -> 200 OK with value
    /// Failure -> ProblemDetails with error
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.ToHttpResult(200, null);
    }

    /// <summary>
    /// Converts Result{T} to IResult with custom status codes.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCodeMapper">Function to map errors to status codes.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        int successStatusCode = 200,
        Func<Error, int>? errorStatusCodeMapper = null)
    {
        if (result.IsSuccess)
        {
            return Results.Json(result.Value, statusCode: successStatusCode);
        }

        var statusCode = errorStatusCodeMapper?.Invoke(result.Error) 
            ?? ErrorStatusCodeMapper.GetStatusCode(result.Error);
        
        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        return Results.Json(problemDetails, statusCode: statusCode);
    }

    /// <summary>
    /// Converts Result{T} to ActionResult (MVC Controllers).
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An ActionResult for MVC controllers.</returns>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        return result.ToActionResult(200, null);
    }

    /// <summary>
    /// Converts Result{T} to ActionResult with custom status codes.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCodeMapper">Function to map errors to status codes.</param>
    /// <returns>An ActionResult for MVC controllers.</returns>
    public static ActionResult<T> ToActionResult<T>(
        this Result<T> result,
        int successStatusCode = 200,
        Func<Error, int>? errorStatusCodeMapper = null)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                200 => new OkObjectResult(result.Value),
                201 => new CreatedResult(string.Empty, result.Value),
                202 => new AcceptedResult(string.Empty, result.Value),
                204 => new NoContentResult(),
                _ => new ObjectResult(result.Value) { StatusCode = successStatusCode }
            };
        }

        var statusCode = errorStatusCodeMapper?.Invoke(result.Error) 
            ?? ErrorStatusCodeMapper.GetStatusCode(result.Error);
        
        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    /// <summary>
    /// Converts non-generic Result to IResult (Minimal API).
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        var statusCode = ErrorStatusCodeMapper.GetStatusCode(result.Error);
        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        return Results.Json(problemDetails, statusCode: statusCode);
    }

    /// <summary>
    /// Converts non-generic Result to IActionResult (MVC Controllers).
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IActionResult for MVC controllers.</returns>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        var statusCode = ErrorStatusCodeMapper.GetStatusCode(result.Error);
        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
