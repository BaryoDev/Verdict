using System;

namespace Upshot;

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
    /// Returns a new error with the specified exception attached.
    /// </summary>
    public Error WithException(Exception exception) =>
        new(Code, Message, exception);
}
