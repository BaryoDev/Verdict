using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Verdict.AspNetCore;

/// <summary>
/// ASP.NET Core extensions for Result types.
/// </summary>
public static class ResultExtensions
{
    private const string ReflectionJsonMessage =
        "Serialises the response body with reflection-based System.Text.Json. Under trimming or Native AOT use the overload that takes a JsonTypeInfo.";

    /// <summary>
    /// Converts Result{T} to IResult (Minimal API).
    /// Success -> 200 OK with value
    /// Failure -> ProblemDetails with error
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    [RequiresUnreferencedCode(ReflectionJsonMessage)]
    [RequiresDynamicCode(ReflectionJsonMessage)]
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
    [RequiresUnreferencedCode(ReflectionJsonMessage)]
    [RequiresDynamicCode(ReflectionJsonMessage)]
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        int successStatusCode = 200,
        Func<Error, int>? errorStatusCodeMapper = null)
    {
        if (result.IsSuccess)
        {
            return ForbidsBody(successStatusCode)
                ? Results.StatusCode(successStatusCode)
                : Results.Json(result.Value, statusCode: successStatusCode);
        }

        var statusCode = errorStatusCodeMapper?.Invoke(result.Error) 
            ?? ErrorStatusCodeMapper.GetStatusCode(result.Error);
        
        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        return Results.Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json");
    }

    /// <summary>
    /// Converts Result{T} to IResult using the configuration registered in this
    /// request's container.
    /// </summary>
    /// <remarks>
    /// The overloads without an <see cref="HttpContext"/> read process-wide
    /// statics, so two applications hosted in one process share one configuration
    /// and the last registration wins. <c>AddVerdictProblemDetails</c> registered
    /// <see cref="IErrorStatusCodeMapper"/> and <see cref="IVerdictProblemDetailsFactory"/>
    /// but nothing resolved them, which is what made the container-scoped half of
    /// that fix unreachable. This is the overload that reaches it.
    /// <para>
    /// Falls back to the statics when neither service is registered, so it is safe
    /// to call from an application that never called <c>AddVerdictProblemDetails</c>.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="context">The current request, used to resolve the registered configuration.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    [RequiresUnreferencedCode(ReflectionJsonMessage)]
    [RequiresDynamicCode(ReflectionJsonMessage)]
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        HttpContext context,
        int successStatusCode = 200)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (result.IsSuccess)
        {
            return ForbidsBody(successStatusCode)
                ? Results.StatusCode(successStatusCode)
                : Results.Json(result.Value, statusCode: successStatusCode);
        }

        var (statusCode, problemDetails) = Describe(context, result.Error);
        return Results.Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json");
    }

    /// <summary>
    /// Converts a non-generic Result to IResult using the configuration
    /// registered in this request's container.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="context">The current request, used to resolve the registered configuration.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 204).</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    [RequiresUnreferencedCode(ReflectionJsonMessage)]
    [RequiresDynamicCode(ReflectionJsonMessage)]
    public static IResult ToHttpResult(
        this Result result,
        HttpContext context,
        int successStatusCode = 204)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (result.IsSuccess)
        {
            return Results.StatusCode(successStatusCode);
        }

        var (statusCode, problemDetails) = Describe(context, result.Error);
        return Results.Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json");
    }

    /// <summary>
    /// Converts Result{T} to IResult, serialising the success value with a
    /// caller-supplied <see cref="JsonTypeInfo{T}"/>. Safe under trimming and
    /// Native AOT.
    /// </summary>
    /// <remarks>
    /// The error body is a problem document written through a source-generated
    /// context, which has limits the reflective overloads do not: properties of a
    /// <see cref="ProblemDetails"/> subclass are not written (only
    /// <see cref="ValidationProblemDetails"/> is recognised), extension values must
    /// be one of string, int, long, bool, double, decimal, Guid, DateTime,
    /// DateTimeOffset, int[] or string[] (any other type throws
    /// <see cref="NotSupportedException"/> when the result executes), and
    /// <c>HttpJsonOptions</c> is not applied. For an arbitrary object, pass
    /// <paramref name="problemDetailsTypeInfo"/> from your own context that lists it.
    /// </remarks>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="jsonTypeInfo">Metadata for <typeparamref name="T"/>, usually from a source-generated context.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCodeMapper">Function to map errors to status codes.</param>
    /// <param name="problemDetailsTypeInfo">Optional metadata for every error body. Null uses the built-in context, which keeps the errors of a ValidationProblemDetails; a value here writes it as a base ProblemDetails.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        JsonTypeInfo<T> jsonTypeInfo,
        int successStatusCode = 200,
        Func<Error, int>? errorStatusCodeMapper = null,
        JsonTypeInfo<ProblemDetails>? problemDetailsTypeInfo = null)
    {
        if (jsonTypeInfo is null) throw new ArgumentNullException(nameof(jsonTypeInfo));

        if (result.IsSuccess)
        {
            return ForbidsBody(successStatusCode)
                ? Results.StatusCode(successStatusCode)
                : Results.Json(result.Value, jsonTypeInfo, statusCode: successStatusCode);
        }

        var statusCode = errorStatusCodeMapper?.Invoke(result.Error)
            ?? ErrorStatusCodeMapper.GetStatusCode(result.Error);
        return SafeProblemResult(ProblemDetailsFactory.CreateFromError(result.Error, statusCode), statusCode, problemDetailsTypeInfo);
    }

    /// <summary>
    /// Converts Result{T} to IResult using the configuration registered in this
    /// request's container, serialising the success value with a caller-supplied
    /// <see cref="JsonTypeInfo{T}"/>. Safe under trimming and Native AOT. The
    /// error body has the limits described on the overload without a context.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="context">The current request, used to resolve the registered configuration.</param>
    /// <param name="jsonTypeInfo">Metadata for <typeparamref name="T"/>, usually from a source-generated context.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="problemDetailsTypeInfo">Optional metadata for every error body. Null uses the built-in context, which keeps the errors of a ValidationProblemDetails; a value here writes it as a base ProblemDetails.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        HttpContext context,
        JsonTypeInfo<T> jsonTypeInfo,
        int successStatusCode = 200,
        JsonTypeInfo<ProblemDetails>? problemDetailsTypeInfo = null)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (jsonTypeInfo is null) throw new ArgumentNullException(nameof(jsonTypeInfo));

        if (result.IsSuccess)
        {
            return ForbidsBody(successStatusCode)
                ? Results.StatusCode(successStatusCode)
                : Results.Json(result.Value, jsonTypeInfo, statusCode: successStatusCode);
        }

        var (statusCode, problemDetails) = Describe(context, result.Error);
        return SafeProblemResult(problemDetails, statusCode, problemDetailsTypeInfo);
    }

    /// <summary>
    /// Converts a non-generic Result to IResult without reflection. Safe under
    /// trimming and Native AOT. The error body has the limits described on
    /// <see cref="ToHttpResult{T}(Result{T}, JsonTypeInfo{T}, int, Func{Error, int}?, JsonTypeInfo{ProblemDetails}?)"/>.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="context">The current request, used to resolve the registered configuration.</param>
    /// <param name="problemDetailsTypeInfo">Optional metadata for every error body. Null uses the built-in context, which keeps the errors of a ValidationProblemDetails; a value here writes it as a base ProblemDetails.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 204).</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    public static IResult ToHttpResult(
        this Result result,
        HttpContext context,
        JsonTypeInfo<ProblemDetails>? problemDetailsTypeInfo,
        int successStatusCode = 204)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (result.IsSuccess)
        {
            return Results.StatusCode(successStatusCode);
        }

        var (statusCode, problemDetails) = Describe(context, result.Error);
        return SafeProblemResult(problemDetails, statusCode, problemDetailsTypeInfo);
    }

    /// <summary>
    /// Converts Result{T} to ActionResult using the configuration registered in
    /// this request's container.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="context">The current request, used to resolve the registered configuration.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <returns>An ActionResult for MVC controllers.</returns>
    public static ActionResult<T> ToActionResult<T>(
        this Result<T> result,
        HttpContext context,
        int successStatusCode = 200)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (result.IsSuccess)
        {
            return ForbidsBody(successStatusCode)
                ? new StatusCodeResult(successStatusCode)
                : new ObjectResult(result.Value) { StatusCode = successStatusCode };
        }

        var (statusCode, problemDetails) = Describe(context, result.Error);
        // RFC 7807 says a problem document is application/problem+json. The
        // minimal API paths have always set it; these three did not, so the same
        // error reached a client differently depending on which one produced it.
        var payload = new ObjectResult(problemDetails) { StatusCode = statusCode };
        payload.ContentTypes.Add("application/problem+json");
        return payload;
    }

    /// <summary>
    /// Resolves the container's mapper and factory, falling back to the statics.
    /// </summary>
    /// <summary>
    /// Whether HTTP forbids a body with this status. Writing the value anyway
    /// made Kestrel throw after the handler returned, so the client got an
    /// empty 204 and the server logged an unhandled exception.
    /// </summary>
    private static bool ForbidsBody(int statusCode) =>
        statusCode == 204 || statusCode == 304 || (statusCode >= 100 && statusCode < 200);

    // Source-generated, so no reflection. A ValidationProblemDetails keeps its
    // errors; any other subclass is written as a plain ProblemDetails.
    private static IResult SafeProblemResult(ProblemDetails details, int statusCode, JsonTypeInfo<ProblemDetails>? typeInfo)
    {
        if (typeInfo is not null)
        {
            return Results.Json(details, typeInfo, "application/problem+json", statusCode);
        }

        return details is ValidationProblemDetails validation
            ? Results.Json(validation, VerdictProblemJsonContext.Default.ValidationProblemDetails, "application/problem+json", statusCode)
            : Results.Json(details, VerdictProblemJsonContext.Default.ProblemDetails, "application/problem+json", statusCode);
    }

    private static (int StatusCode, ProblemDetails Details) Describe(HttpContext context, Error error)
    {
        var services = context.RequestServices;

        var mapper = services?.GetService(typeof(IErrorStatusCodeMapper)) as IErrorStatusCodeMapper;
        var statusCode = mapper is not null
            ? mapper.GetStatusCode(error)
            : ErrorStatusCodeMapper.GetStatusCode(error);

        var factory = services?.GetService(typeof(IVerdictProblemDetailsFactory)) as IVerdictProblemDetailsFactory;
        var details = factory is not null
            ? factory.CreateFromError(error, statusCode)
            : ProblemDetailsFactory.CreateFromError(error, statusCode);

        return (statusCode, details);
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
    /// Converts Result{T} to ActionResult with custom status codes and optional location URI.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCodeMapper">Function to map errors to status codes.</param>
    /// <param name="locationUri">Location URI for 201 Created or 202 Accepted responses.</param>
    /// <returns>An ActionResult for MVC controllers.</returns>
    public static ActionResult<T> ToActionResult<T>(
        this Result<T> result,
        int successStatusCode = 200,
        Func<Error, int>? errorStatusCodeMapper = null,
        string? locationUri = null)
    {
        if (result.IsSuccess)
        {
            if (ForbidsBody(successStatusCode))
            {
                return successStatusCode == 204
                    ? new NoContentResult()
                    : new StatusCodeResult(successStatusCode);
            }

            if (locationUri != null)
            {
                return successStatusCode switch
                {
                    201 => new CreatedResult(locationUri, result.Value),
                    202 => new AcceptedResult(locationUri, result.Value),
                    _ => new ObjectResult(result.Value) { StatusCode = successStatusCode }
                };
            }

            return successStatusCode switch
            {
                200 => new OkObjectResult(result.Value),
                201 => new ObjectResult(result.Value) { StatusCode = 201 },
                202 => new ObjectResult(result.Value) { StatusCode = 202 },
                _ => new ObjectResult(result.Value) { StatusCode = successStatusCode }
            };
        }

        var statusCode = errorStatusCodeMapper?.Invoke(result.Error)
            ?? ErrorStatusCodeMapper.GetStatusCode(result.Error);

        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        // RFC 7807 says a problem document is application/problem+json. The
        // minimal API paths have always set it; these three did not, so the same
        // error reached a client differently depending on which one produced it.
        var payload = new ObjectResult(problemDetails) { StatusCode = statusCode };
        payload.ContentTypes.Add("application/problem+json");
        return payload;
    }

    /// <summary>
    /// Converts non-generic Result to IResult (Minimal API).
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IResult for Minimal API endpoints.</returns>
    [RequiresUnreferencedCode(ReflectionJsonMessage)]
    [RequiresDynamicCode(ReflectionJsonMessage)]
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        var statusCode = ErrorStatusCodeMapper.GetStatusCode(result.Error);
        var problemDetails = ProblemDetailsFactory.CreateFromError(result.Error, statusCode);
        return Results.Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json");
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
        // RFC 7807 says a problem document is application/problem+json. The
        // minimal API paths have always set it; these three did not, so the same
        // error reached a client differently depending on which one produced it.
        var payload = new ObjectResult(problemDetails) { StatusCode = statusCode };
        payload.ContentTypes.Add("application/problem+json");
        return payload;
    }
}
