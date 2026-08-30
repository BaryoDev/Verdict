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
        Message = Normalize(message);
        Exception = exception;
    }

    /// <summary>
    /// The longest message an <see cref="Error"/> will carry. Anything longer is
    /// truncated at construction.
    /// </summary>
    /// <remarks>
    /// A message is read by a person. Nothing needs four kilobytes, and without a
    /// bound a request value interpolated into an error carried whatever size the
    /// caller sent, all the way into the log and the response body.
    /// </remarks>
    public const int MaxMessageLength = 4096;

    /// <summary>
    /// The marker left in place of the text removed by truncation.
    /// </summary>
    public const string TruncationMarker = "... [truncated]";

    /// <summary>
    /// Removes control characters and bounds the length.
    /// </summary>
    /// <remarks>
    /// This runs on every error, including the hot failure path, so it allocates
    /// nothing unless it actually has to change something: a clean message is
    /// scanned and returned as it arrived.
    /// <para>
    /// Control characters are removed rather than rejected. A carriage return in
    /// a message forges a line in any plain-text log sink, and throwing instead
    /// would turn a logging concern into a crash at the worst possible moment.
    /// The error code still throws on bad input, because a code is chosen by the
    /// programmer and a message usually is not.
    /// </para>
    /// </remarks>
    private static string Normalize(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var needsStrip = false;
        for (var i = 0; i < message!.Length; i++)
        {
            var c = message[i];
            if (c != '\t' && (c < ' ' || c == '\u007f'))
            {
                needsStrip = true;
                break;
            }
        }

        if (!needsStrip && message.Length <= MaxMessageLength)
        {
            return message;
        }

        var limit = message.Length <= MaxMessageLength ? message.Length : MaxMessageLength;
        var builder = new System.Text.StringBuilder(limit + TruncationMarker.Length);

        for (var i = 0; i < limit; i++)
        {
            var c = message[i];
            builder.Append(c != '\t' && (c < ' ' || c == '\u007f') ? ' ' : c);
        }

        if (message.Length > MaxMessageLength)
        {
            builder.Append(TruncationMarker);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The code carried by an error built from an exception when the caller did
    /// not choose one.
    /// </summary>
    /// <remarks>
    /// A constant rather than the exception's type name. The type name identifies
    /// the data access stack and often the vendor, and it used to reach clients
    /// through the ProblemDetails <c>errorCode</c> extension, which is on by
    /// default, while the option that exists to hide exception detail is off by
    /// default. One field was smuggling another. Callers who want the type still
    /// have <see cref="Exception"/>.
    /// </remarks>
    public const string UnhandledExceptionCode = "UNHANDLED_EXCEPTION";

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
    /// WARNING: This method exposes the raw exception message which may contain sensitive information.
    /// Consider using FromException(exception, sanitize: true) in production environments.
    /// </summary>
    [Obsolete("This method exposes raw exception messages. Use FromException(exception, sanitize: true) in production to prevent information leakage.")]
    public static Error FromException(Exception exception) =>
        new(UnhandledExceptionCode, exception.Message, exception);

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

        return new(UnhandledExceptionCode, message, exception);
    }

    /// <summary>
    /// Creates a new error from an exception with a custom error code.
    /// WARNING: This method exposes the raw exception message which may contain sensitive information.
    /// Consider using FromException(exception, code, sanitize: true) in production environments.
    /// </summary>
    /// <param name="exception">The exception to create an error from.</param>
    /// <param name="code">The error code to use instead of the exception type name.</param>
    /// <returns>A new Error instance.</returns>
    [Obsolete("This method exposes raw exception messages. Use FromException(exception, code, sanitize: true) in production to prevent information leakage.")]
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
    /// Returns a short description of the error: <c>[CODE] message</c>, with the
    /// exception type name appended when one is attached.
    /// </summary>
    /// <remarks>
    /// Written by hand rather than left to the compiler. A record struct's
    /// generated <c>ToString</c> renders every property, including
    /// <see cref="Exception"/>, and an exception renders as its own message and
    /// stack. That defeated <see cref="FromException(Exception, bool, string?)"/>
    /// with <c>sanitize: true</c> entirely, because the original message came
    /// back out beside the sanitised one the moment anything logged the error.
    /// It was also the most expensive operation in the package, at 1,544 bytes
    /// per call with an exception attached against 176 here.
    /// <para>
    /// The exception type name is kept because it is useful in a log and is not
    /// the message. Callers who need the exception itself have
    /// <see cref="Exception"/>.
    /// </para>
    /// </remarks>
    public override string ToString() =>
        Exception is null
            ? $"[{Code}] {Message}"
            : $"[{Code}] {Message} (+{Exception.GetType().Name})";

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
                return false;
        }
        return true;
    }
}
