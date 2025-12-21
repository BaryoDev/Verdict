using System;
using System.Collections.Generic;

namespace Verdict.AspNetCore;

/// <summary>
/// Maps common error codes to HTTP status codes.
/// </summary>
public static class ErrorStatusCodeMapper
{
    private static readonly Dictionary<string, int> _customMappings = new();

    private static readonly Dictionary<string, int> _defaultMappings = new()
    {
        // 4xx Client Errors
        { "BAD_REQUEST", 400 },
        { "INVALID_INPUT", 400 },
        { "VALIDATION_ERROR", 400 },
        { "VALIDATION_FAILED", 400 },
        
        { "UNAUTHORIZED", 401 },
        { "UNAUTHENTICATED", 401 },
        { "INVALID_CREDENTIALS", 401 },
        { "INVALID_TOKEN", 401 },
        
        { "FORBIDDEN", 403 },
        { "ACCESS_DENIED", 403 },
        { "INSUFFICIENT_PERMISSIONS", 403 },
        
        { "NOT_FOUND", 404 },
        { "RESOURCE_NOT_FOUND", 404 },
        { "USER_NOT_FOUND", 404 },
        { "ENTITY_NOT_FOUND", 404 },
        
        { "CONFLICT", 409 },
        { "DUPLICATE", 409 },
        { "DUPLICATE_EMAIL", 409 },
        { "DUPLICATE_USERNAME", 409 },
        { "ALREADY_EXISTS", 409 },
        
        { "UNPROCESSABLE_ENTITY", 422 },
        { "INVALID_STATE", 422 },
        
        { "PAYMENT_REQUIRED", 402 },
        { "RATE_LIMITED", 429 },
        { "TOO_MANY_REQUESTS", 429 },
        
        // 5xx Server Errors
        { "INTERNAL_ERROR", 500 },
        { "SERVER_ERROR", 500 },
        { "DATABASE_ERROR", 500 },
        { "EXTERNAL_SERVICE_ERROR", 502 },
        { "SERVICE_UNAVAILABLE", 503 },
        { "TIMEOUT", 504 }
    };

    /// <summary>
    /// Default mapping: NOT_FOUND -> 404, UNAUTHORIZED -> 401, etc.
    /// </summary>
    /// <param name="error">The error to map.</param>
    /// <returns>HTTP status code.</returns>
    public static int GetStatusCode(Error error)
    {
        // Check custom mappings first
        if (_customMappings.TryGetValue(error.Code, out var customCode))
        {
            return customCode;
        }

        // Check default mappings
        if (_defaultMappings.TryGetValue(error.Code, out var defaultCode))
        {
            return defaultCode;
        }

        // Default to 400 for unknown errors
        return 400;
    }

    /// <summary>
    /// Register custom error code mappings.
    /// </summary>
    /// <param name="errorCode">The error code to map.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public static void RegisterMapping(string errorCode, int statusCode)
    {
        if (string.IsNullOrEmpty(errorCode))
        {
            throw new ArgumentNullException(nameof(errorCode));
        }

        if (statusCode < 100 || statusCode > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), "Status code must be between 100 and 599.");
        }

        _customMappings[errorCode] = statusCode;
    }

    /// <summary>
    /// Clear all custom mappings.
    /// </summary>
    public static void ClearCustomMappings()
    {
        _customMappings.Clear();
    }

    /// <summary>
    /// Get all registered custom mappings.
    /// </summary>
    /// <returns>Dictionary of custom error code to status code mappings.</returns>
    public static IReadOnlyDictionary<string, int> GetCustomMappings()
    {
        return _customMappings;
    }
}
