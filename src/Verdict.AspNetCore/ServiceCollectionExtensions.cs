using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
        if (services is null) throw new ArgumentNullException(nameof(services));

        return services.AddVerdictCore(new VerdictProblemDetailsOptions(), configure);
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
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (environment == null) throw new ArgumentNullException(nameof(environment));

        var options = new VerdictProblemDetailsOptions
        {
            IncludeExceptionDetails = environment.IsDevelopment(),
            IncludeStackTrace = environment.IsDevelopment(),
            IncludeErrorMessage = true,
            IncludeErrorCode = true
        };

        return services.AddVerdictCore(options, configure);
    }

    /// <summary>
    /// Registers the options and the services that read them.
    /// </summary>
    /// <remarks>
    /// Previously this method registered nothing and only assigned a static, so
    /// two hosts in one process shared a single configuration and the last
    /// registration won. Options and services are now container-scoped. The
    /// static default is still assigned so the parameterless extension methods,
    /// which have no access to DI, keep behaving as configured.
    /// </remarks>
    private static IServiceCollection AddVerdictCore(
        this IServiceCollection services,
        VerdictProblemDetailsOptions options,
        Action<VerdictProblemDetailsOptions>? configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        configure?.Invoke(options);

        services.AddSingleton<IOptions<VerdictProblemDetailsOptions>>(
            new OptionsWrapper<VerdictProblemDetailsOptions>(options));
        services.AddSingleton<IErrorStatusCodeMapper, OptionsErrorStatusCodeMapper>();
        services.AddSingleton<IVerdictProblemDetailsFactory, OptionsProblemDetailsFactory>();

        // Kept so the parameterless ToActionResult/ToHttpResult overloads, which
        // cannot reach the container, still honour this configuration.
        ProblemDetailsFactory.SetDefaultOptions(options);

        return services;
    }
}
