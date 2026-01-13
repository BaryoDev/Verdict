using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Verdict.AspNetCore;

/// <summary>
/// Extension methods for configuring Verdict services in ASP.NET Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Verdict ProblemDetails options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVerdictProblemDetails(
        this IServiceCollection services,
        Action<VerdictProblemDetailsOptions>? configure = null)
    {
        var options = new VerdictProblemDetailsOptions();
        configure?.Invoke(options);
        ProblemDetailsFactory.SetDefaultOptions(options);
        return services;
    }

    /// <summary>
    /// Configures Verdict ProblemDetails options with environment-aware defaults.
    /// In development, exception details are included. In production, they are hidden.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <param name="configure">Optional additional configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVerdictProblemDetails(
        this IServiceCollection services,
        IHostEnvironment environment,
        Action<VerdictProblemDetailsOptions>? configure = null)
    {
        if (environment == null) throw new ArgumentNullException(nameof(environment));

        var options = new VerdictProblemDetailsOptions
        {
            IncludeExceptionDetails = environment.IsDevelopment(),
            IncludeStackTrace = environment.IsDevelopment(),
            IncludeErrorMessage = true,
            IncludeErrorCode = true
        };

        configure?.Invoke(options);
        ProblemDetailsFactory.SetDefaultOptions(options);
        return services;
    }
}
