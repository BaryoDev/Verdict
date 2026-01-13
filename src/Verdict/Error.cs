using System;

namespace Verdict;

/// <summary>
/// Represents an error with a code and message.
/// Implemented as a readonly record struct for zero-allocation error handling.
/// </summary>
public readonly record struct Error
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception that caused this error, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that caused this error, if any.</param>
    public Error(string code, string message, Exception? exception = null)
    {
        Code = code ?? string.Empty;
        Message = message ?? string.Empty;
        Exception = exception;
    }

    /// <summary>
    /// Creates a new error with the specified code and message.
    /// </summary>
    public static Error Create(string code, string message) => new(code, message);

    /// <summary>
    /// Creates a new error with the specified code, message, and exception.
    /// </summary>
    public static Error Create(string code, string message, Exception exception) => 
        new(code, message, exception);

    /// <summary>
    /// Creates a new error from an exception.
    /// </summary>
    public static Error FromException(Exception exception) =>
        new(exception.GetType().Name, exception.Message, exception);

    /// <summary>
    /// Creates a new error from an exception with optional sanitization.
    /// When sanitized, the exception message is replaced with a generic message to prevent
    /// sensitive information leakage in production environments.
    /// </summary>
    /// <param name="exception">The exception to create an error from.</param>
    /// <param name="sanitize">If true, replaces the exception message with a sanitized version.</param>
    /// <param name="sanitizedMessage">The message to use when sanitizing. Defaults to "An error occurred."</param>
    /// <returns>A new Error instance.</returns>
    public static Error FromException(Exception exception, bool sanitize, string? sanitizedMessage = null)
    {
        if (exception == null) throw new ArgumentNullException(nameof(exception));

        var message = sanitize
            ? sanitizedMessage ?? "An error occurred."
            : exception.Message;

        return new(exception.GetType().Name, message, exception);
    }

    /// <summary>
    /// Creates a new error from an exception with a custom error code.
    /// </summary>
    /// <param name="exception">The exception to create an error from.</param>
    /// <param name="code">The error code to use instead of the exception type name.</param>
    /// <returns>A new Error instance.</returns>
    public static Error FromException(Exception exception, string code) =>
        new(code, exception.Message, exception);

    /// <summary>
    /// Creates a new error from an exception with a custom error code and sanitization.
    /// </summary>
    /// <param name="exception">The exception to create an error from.</param>
    /// <param name="code">The error code to use.</param>
    /// <param name="sanitize">If true, replaces the exception message with a sanitized version.</param>
    /// <param name="sanitizedMessage">The message to use when sanitizing. Defaults to "An error occurred."</param>
    /// <returns>A new Error instance.</returns>
    public static Error FromException(Exception exception, string code, bool sanitize, string? sanitizedMessage = null)
    {
        if (exception == null) throw new ArgumentNullException(nameof(exception));
        if (string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));

        var message = sanitize
            ? sanitizedMessage ?? "An error occurred."
            : exception.Message;

        return new(code, message, exception);
    }

    /// <summary>
    /// Returns a new error with the specified exception attached.
    /// </summary>
    public Error WithException(Exception exception) =>
        new(Code, Message, exception);

    /// <summary>
    /// Creates a new error with validated code and message.
    /// The code is validated to contain only alphanumeric characters and underscores.
    /// </summary>
    /// <param name="code">The error code (must be alphanumeric with underscores only).</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new Error instance with validated code.</returns>
    /// <exception cref="ArgumentException">Thrown when the code contains invalid characters.</exception>
    public static Error CreateValidated(string code, string message)
    {
        ValidateErrorCode(code);
        return new(code, message);
    }

    /// <summary>
    /// Creates a new error with validated code, message, and exception.
    /// The code is validated to contain only alphanumeric characters and underscores.
    /// </summary>
    /// <param name="code">The error code (must be alphanumeric with underscores only).</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that caused this error.</param>
    /// <returns>A new Error instance with validated code.</returns>
    /// <exception cref="ArgumentException">Thrown when the code contains invalid characters.</exception>
    public static Error CreateValidated(string code, string message, Exception exception)
    {
        ValidateErrorCode(code);
        return new(code, message, exception);
    }

    /// <summary>
    /// Validates that an error code contains only valid characters (alphanumeric and underscores).
    /// </summary>
    /// <param name="code">The error code to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when code is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the code contains invalid characters.</exception>
    public static void ValidateErrorCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentNullException(nameof(code), "Error code cannot be null or empty.");
        }

        foreach (var c in code)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                throw new ArgumentException(
                    $"Error code '{code}' contains invalid character '{c}'. " +
                    "Only alphanumeric characters and underscores are allowed.",
                    nameof(code));
            }
        }
    }

    /// <summary>
    /// Checks if an error code is valid (contains only alphanumeric characters and underscores).
    /// </summary>
    /// <param name="code">The error code to check.</param>
    /// <returns>True if the code is valid; otherwise, false.</returns>
    public static bool IsValidErrorCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        foreach (var c in code)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }
}
