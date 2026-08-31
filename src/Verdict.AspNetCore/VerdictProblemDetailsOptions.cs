namespace Verdict.AspNetCore;

/// <summary>
/// Options for configuring ProblemDetails generation from Verdict errors.
/// </summary>
public class VerdictProblemDetailsOptions
{
    /// <summary>
    /// Gets or sets whether to include exception type information in ProblemDetails extensions.
    /// Should be false in production to avoid leaking implementation details.
    /// Default is false.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include the error code in ProblemDetails extensions.
    /// Default is true.
    /// </summary>
    public bool IncludeErrorCode { get; set; } = true;

    /// <summary>
    /// Gets error code to HTTP status code mappings for this container.
    /// </summary>
    /// <remarks>
    /// Checked before the shared defaults in <see cref="ErrorStatusCodeMapper"/>,
    /// so two hosts in one process can map the same code differently. Prefer
    /// this over the static <c>RegisterMapping</c>, which is process-wide.
    /// </remarks>
    public System.Collections.Generic.Dictionary<string, int> StatusCodeMappings { get; } = new();

    /// <summary>
    /// Gets or sets whether to include the error message as the ProblemDetails detail.
    /// If false, a generic message will be used for server errors (5xx).
    /// Default is true.
    /// </summary>
    public bool IncludeErrorMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets the message sent in place of a suppressed one.
    /// Default is "An unexpected error occurred."
    /// </summary>
    /// <remarks>
    /// Applies whenever the real message is withheld, which is now any status
    /// code rather than only 5xx. Before, suppression keyed on the status, so an
    /// exception-derived error that mapped to 400 was never suppressed at all.
    /// </remarks>
    public string GenericErrorMessage { get; set; } = "An unexpected error occurred.";

    /// <summary>
    /// Gets or sets the message sent in place of a suppressed one.
    /// </summary>
    [System.Obsolete("Renamed to GenericErrorMessage, because it now applies to any suppressed message rather than only server errors.")]
    public string GenericServerErrorMessage
    {
        get => GenericErrorMessage;
        set => GenericErrorMessage = value;
    }

    /// <summary>
    /// Gets or sets whether to include stack trace information for exceptions.
    /// Should only be true in development environments.
    /// Default is false.
    /// </summary>
    public bool IncludeStackTrace { get; set; } = false;
}
