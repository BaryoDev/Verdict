using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Verdict.AspNetCore;

/// <summary>
/// Factory for creating RFC 7807 ProblemDetails from Error.
/// </summary>
public static class ProblemDetailsFactory
{
    private static VerdictProblemDetailsOptions _defaultOptions = new();

    /// <summary>
    /// Sets the default options for ProblemDetails generation.
    /// Thread-safe via Interlocked.Exchange.
    /// </summary>
    /// <param name="options">The options to use as default.</param>
    public static void SetDefaultOptions(VerdictProblemDetailsOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        Interlocked.Exchange(ref _defaultOptions, options);
    }

    /// <summary>
    /// Restores the process-wide defaults.
    /// </summary>
    /// <remarks>
    /// The default options are static, so a test that configures them affects
    /// every test that runs afterwards in the same process. Call this in
    /// teardown, or prefer the DI-scoped
    /// <see cref="IVerdictProblemDetailsFactory"/>, which reads options from a
    /// container instead.
    /// </remarks>
    public static void ResetDefaultOptions()
    {
        Interlocked.Exchange(ref _defaultOptions, new VerdictProblemDetailsOptions());
    }

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
        // Read once to ensure consistent options during the call
        var options = Volatile.Read(ref _defaultOptions);
        return CreateFromError(error, statusCode, options);
    }

    /// <summary>
    /// Creates ProblemDetails from an Error with custom options.
    /// Maps Error.Code to ProblemDetails.Type
    /// Maps Error.Message to ProblemDetails.Detail
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <param name="statusCode">HTTP status code (default: 400).</param>
    /// <param name="options">Options for controlling what information is included.</param>
    /// <returns>RFC 7807 compliant ProblemDetails.</returns>
    public static ProblemDetails CreateFromError(Error error, int statusCode, VerdictProblemDetailsOptions options)
    {
        options ??= _defaultOptions;

        // A message that came from an exception was written for an operator, not
        // for a client, so IncludeExceptionDetails governs it. Suppression used to
        // key on statusCode >= 500, and an exception-derived error mapped to 400,
        // so IncludeErrorMessage=false could not suppress the one thing it existed for.
        var carriesException = error.Exception is not null;
        var suppressMessage = !options.IncludeErrorMessage
            || (carriesException && !options.IncludeExceptionDetails);

        var detail = suppressMessage ? options.GenericErrorMessage : error.Message;

        var problemDetails = new ProblemDetails
        {
            Type = GetProblemType(statusCode),
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = detail
        };

        // Add error code as extension. Withheld along with the message when the
        // error carries an exception and exception detail is off, because a code
        // chosen by the caller for an exception path can still name internals.
        if (options.IncludeErrorCode && !(carriesException && !options.IncludeExceptionDetails))
        {
            problemDetails.Extensions["errorCode"] = error.Code;
        }

        // Add exception details if present and allowed
        if (error.Exception != null && options.IncludeExceptionDetails)
        {
            problemDetails.Extensions["exceptionType"] = error.Exception.GetType().Name;
            
            if (options.IncludeStackTrace)
            {
                problemDetails.Extensions["stackTrace"] = error.Exception.StackTrace;
            }
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
        // Reading a disposed collection throws, and this runs while the handler is
        // already building an error response, so throwing here turns a reported
        // validation failure into an unhandled 500 and loses the errors entirely.
        // Report what is knowable instead.
        if (result.ErrorsDisposed)
        {
            return new ValidationProblemDetails(new Dictionary<string, string[]>(0))
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = 400,
                Detail = "The validation errors were released before the response was built. "
                    + "Build the response before calling DisposeErrors.",
            };
        }

        // Pre-allocate array with exact size to avoid List resizing
        var errorCount = result.ErrorCount;
        var errorMessages = new string[errorCount];

        int index = 0;
        foreach (var error in result.Errors)
        {
            errorMessages[index++] = $"[{error.Code}] {error.Message}";
        }

        var errors = new Dictionary<string, string[]>(1)
        {
            ["errors"] = errorMessages
        };

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = 400,
            Detail = $"{errorCount} validation error(s) occurred."
        };
    }

    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            402 => "https://tools.ietf.org/html/rfc9110#section-15.5.3",
            403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
            429 => "https://tools.ietf.org/html/rfc6585#section-4",
            500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
            503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
            504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
            _ => "https://tools.ietf.org/html/rfc9110#section-15"
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            402 => "Payment Required",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Error"
        };
    }
}
