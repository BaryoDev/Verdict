using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Verdict.AspNetCore;

/// <summary>
/// Source-generated metadata for the problem documents this package writes, so
/// the error path of <c>ToHttpResult</c> needs no reflection.
/// </summary>
/// <remarks>
/// <c>string</c> is listed because <c>ProblemDetails.Extensions</c> holds
/// <c>object</c> values (the error code, exception type and stack trace are all
/// strings) and System.Text.Json resolves each value's runtime type from this
/// context.
/// <para>
/// Excluded from coverage: the serializer code is generated, and coverlet reports
/// its path-mapped files (<c>/_/src/...</c>) next to real ones, which changes how
/// it relativises every other file name and breaks the merge in
/// <c>build/coverage-gate.py</c>. The attribute on this declaration covers the
/// generated parts of the partial class too.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(string))]
internal sealed partial class VerdictProblemJsonContext : JsonSerializerContext
{
}
