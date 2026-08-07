using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Verdict;

namespace Verdict.AspNetCore;

/// <summary>
/// Status code mapper backed by the options registered in a container.
/// </summary>
internal sealed class OptionsErrorStatusCodeMapper : IErrorStatusCodeMapper
{
    private readonly IReadOnlyDictionary<string, int> _mappings;

    public OptionsErrorStatusCodeMapper(IOptions<VerdictProblemDetailsOptions> options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        _mappings = options.Value.StatusCodeMappings;
    }

    public int GetStatusCode(Error error)
    {
        // Container-scoped mappings win, then the shared defaults.
        if (!string.IsNullOrEmpty(error.Code) && _mappings.TryGetValue(error.Code, out var code))
        {
            return code;
        }

        return ErrorStatusCodeMapper.GetStatusCode(error);
    }
}

/// <summary>
/// ProblemDetails factory backed by the options registered in a container.
/// </summary>
internal sealed class OptionsProblemDetailsFactory : IVerdictProblemDetailsFactory
{
    private readonly VerdictProblemDetailsOptions _options;

    public OptionsProblemDetailsFactory(IOptions<VerdictProblemDetailsOptions> options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        _options = options.Value;
    }

    public ProblemDetails CreateFromError(Error error, int statusCode = 400) =>
        ProblemDetailsFactory.CreateFromError(error, statusCode, _options);
}
