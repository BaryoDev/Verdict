using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Verdict.AspNetCore;
using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// AddVerdictProblemDetails took an IServiceCollection, registered nothing, and
/// mutated a process-wide static. Two hosts in one process therefore shared one
/// configuration and the last registration won. These pin the DI behaviour.
/// </summary>
public class DiScopedConfigurationTests : IDisposable
{
    // AddVerdictProblemDetails also assigns the process-wide default so the
    // parameterless extension methods keep working. That leaks between tests,
    // which is the very hazard these tests exist to document.
    public void Dispose() => ProblemDetailsFactory.ResetDefaultOptions();

    [Fact]
    public void AddVerdictProblemDetails_RegistersOptionsInTheContainer()
    {
        var services = new ServiceCollection();
        services.AddVerdictProblemDetails(o => o.IncludeErrorCode = false);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<VerdictProblemDetailsOptions>>().Value;

        Assert.False(options.IncludeErrorCode);
    }

    [Fact]
    public void TwoContainers_DoNotShareConfiguration()
    {
        var a = new ServiceCollection();
        a.AddVerdictProblemDetails(o => o.IncludeErrorMessage = false);
        var providerA = a.BuildServiceProvider();

        var b = new ServiceCollection();
        b.AddVerdictProblemDetails(o => o.IncludeErrorMessage = true);
        var providerB = b.BuildServiceProvider();

        Assert.False(providerA.GetRequiredService<IOptions<VerdictProblemDetailsOptions>>().Value.IncludeErrorMessage);
        Assert.True(providerB.GetRequiredService<IOptions<VerdictProblemDetailsOptions>>().Value.IncludeErrorMessage);
    }

    [Fact]
    public void AddVerdictProblemDetails_RegistersTheStatusCodeMapper()
    {
        var services = new ServiceCollection();
        services.AddVerdictProblemDetails();

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IErrorStatusCodeMapper>();

        Assert.Equal(404, mapper.GetStatusCode(new Error("NOT_FOUND", "missing")));
    }

    [Fact]
    public void CustomMappings_AreScopedToTheContainer()
    {
        var a = new ServiceCollection();
        a.AddVerdictProblemDetails(o => o.StatusCodeMappings["TENANT_A_ONLY"] = 418);
        var mapperA = a.BuildServiceProvider().GetRequiredService<IErrorStatusCodeMapper>();

        var b = new ServiceCollection();
        b.AddVerdictProblemDetails();
        var mapperB = b.BuildServiceProvider().GetRequiredService<IErrorStatusCodeMapper>();

        Assert.Equal(418, mapperA.GetStatusCode(new Error("TENANT_A_ONLY", "x")));
        Assert.NotEqual(418, mapperB.GetStatusCode(new Error("TENANT_A_ONLY", "x")));
    }

    [Fact]
    public void Mapper_FallsBackForUnknownCodes()
    {
        var services = new ServiceCollection();
        services.AddVerdictProblemDetails();
        var mapper = services.BuildServiceProvider().GetRequiredService<IErrorStatusCodeMapper>();

        Assert.Equal(400, mapper.GetStatusCode(new Error("SOMETHING_UNMAPPED", "x")));
    }

    [Fact]
    public void ProblemDetailsFactory_CanBeResolvedAndUsesContainerOptions()
    {
        var services = new ServiceCollection();
        services.AddVerdictProblemDetails(o => o.IncludeErrorCode = false);
        var factory = services.BuildServiceProvider().GetRequiredService<IVerdictProblemDetailsFactory>();

        var problem = factory.CreateFromError(new Error("SOME_CODE", "message"), 400);

        Assert.False(problem.Extensions.ContainsKey("errorCode"));
    }

    /// <summary>
    /// The static path stays supported so existing code keeps working.
    /// </summary>
    [Fact]
    public void StaticApi_StillWorks()
    {
        ProblemDetailsFactory.SetDefaultOptions(new VerdictProblemDetailsOptions { IncludeErrorCode = true });

        var problem = ProblemDetailsFactory.CreateFromError(new Error("STATIC_CODE", "message"), 400);

        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public void AddVerdictProblemDetails_NullServices_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddVerdictProblemDetails());
    }
}
