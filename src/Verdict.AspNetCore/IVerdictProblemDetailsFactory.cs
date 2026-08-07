using Microsoft.AspNetCore.Mvc;
using Verdict;

namespace Verdict.AspNetCore;

/// <summary>
/// Creates RFC 7807 ProblemDetails from an <see cref="Error"/>.
/// </summary>
/// <remarks>
/// Resolve this from DI to use the options registered in that container. The
/// static <see cref="ProblemDetailsFactory"/> remains available.
/// </remarks>
public interface IVerdictProblemDetailsFactory
{
    /// <summary>
    /// Creates ProblemDetails for an error and status code.
    /// </summary>
    ProblemDetails CreateFromError(Error error, int statusCode = 400);
}
