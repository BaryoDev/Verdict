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
    /// Gets or sets whether to include the error message as the ProblemDetails detail.
    /// If false, a generic message will be used for server errors (5xx).
    /// Default is true.
    /// </summary>
    public bool IncludeErrorMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets the generic message to use for server errors when IncludeErrorMessage is false.
    /// Default is "An unexpected error occurred."
    /// </summary>
    public string GenericServerErrorMessage { get; set; } = "An unexpected error occurred.";

    /// <summary>
    /// Gets or sets whether to include stack trace information for exceptions.
    /// Should only be true in development environments.
    /// Default is false.
    /// </summary>
    public bool IncludeStackTrace { get; set; } = false;
}
