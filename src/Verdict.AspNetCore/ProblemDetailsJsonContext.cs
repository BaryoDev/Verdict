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
/// </remarks>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(string))]
internal sealed partial class VerdictProblemJsonContext : JsonSerializerContext
{
}
