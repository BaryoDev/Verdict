using System;

namespace Upshot.Rich;

/// <summary>
/// Global configuration for Result behavior.
/// </summary>
public static class ResultConfiguration
{
    /// <summary>
    /// Gets or sets the default exception handler used by Try operations.
    /// </summary>
    public static Func<Exception, Error>? DefaultExceptionHandler { get; set; }

    /// <summary>
    /// Gets or sets the default success factory used when creating success messages.
    /// </summary>
    public static Func<string, SuccessInfo>? DefaultSuccessFactory { get; set; }

    /// <summary>
    /// Configures global Result behavior.
    /// </summary>
    public static void Configure(Action<ResultConfigurationBuilder> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var builder = new ResultConfigurationBuilder();
        configure(builder);

        DefaultExceptionHandler = builder.ExceptionHandler;
        DefaultSuccessFactory = builder.SuccessFactory;
    }
}

/// <summary>
/// Builder for configuring global Result behavior.
/// </summary>
public class ResultConfigurationBuilder
{
    internal Func<Exception, Error>? ExceptionHandler { get; private set; }
    internal Func<string, SuccessInfo>? SuccessFactory { get; private set; }

    /// <summary>
    /// Sets the default exception handler.
    /// </summary>
    public ResultConfigurationBuilder UseExceptionHandler(Func<Exception, Error> handler)
    {
        ExceptionHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>
    /// Sets the default success factory.
    /// </summary>
    public ResultConfigurationBuilder UseSuccessFactory(Func<string, SuccessInfo> factory)
    {
        SuccessFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }
}
