using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Verdict.AspNetCore;

/// <summary>
/// Source-generated metadata for the problem documents this package writes, so
/// the error path of <c>ToHttpResult</c> needs no reflection.
/// </summary>
/// <remarks>
/// The primitive types below are listed because <c>ProblemDetails.Extensions</c>
/// holds <c>object</c> values and System.Text.Json resolves each value's runtime
/// type from this context. A type that is not listed throws
/// <see cref="System.NotSupportedException"/> when the result executes.
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
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class VerdictProblemJsonContext : JsonSerializerContext
{
}
