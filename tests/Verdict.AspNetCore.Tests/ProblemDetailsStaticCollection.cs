using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// Serialises every test class that touches <c>ProblemDetailsFactory</c>'s process-wide default
/// options.
///
/// The parameterless <c>CreateFromError</c> overloads read a static, and <c>SetDefaultOptions</c>
/// and <c>AddVerdictProblemDetails</c> both write it. xUnit runs test classes in parallel by
/// default, so a class mutating that static can run at the same instant as a class reading it, and
/// per-class cleanup cannot help: the damage happens while both are still running.
///
/// That is what made #33 look intermittent. It is not flaky in the usual sense, it is
/// order-dependent: it passes on an incremental build and fails after a clean one, because a clean
/// build changes assembly and collection ordering and therefore which class ran last.
///
/// A collection is the honest fix rather than a workaround. The static is real shared state that
/// exists for backwards compatibility, so tests covering it genuinely cannot run concurrently.
/// The container-scoped API added in 2.7.0 is the way out for consumers; these tests still have to
/// cover the static path while it ships.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ProblemDetailsStaticCollection
{
    public const string Name = "ProblemDetailsFactory static defaults";
}
