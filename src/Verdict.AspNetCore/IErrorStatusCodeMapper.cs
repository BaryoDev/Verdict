using Verdict;

namespace Verdict.AspNetCore;

/// <summary>
/// Maps an <see cref="Error"/> to an HTTP status code.
/// </summary>
/// <remarks>
/// Resolve this from DI so mappings belong to a container rather than to the
/// process. The static <see cref="ErrorStatusCodeMapper"/> remains available and
/// is what the parameterless extension methods use.
/// </remarks>
public interface IErrorStatusCodeMapper
{
    /// <summary>
    /// Returns the status code for an error, falling back to 400.
    /// </summary>
    int GetStatusCode(Error error);
}
