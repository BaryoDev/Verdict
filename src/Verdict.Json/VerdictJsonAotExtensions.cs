using System;
using System.Text.Json;
using Verdict;

namespace Verdict.Json;

/// <summary>
/// Converter registration that works under trimming and Native AOT.
/// </summary>
/// <remarks>
/// <see cref="ResultJsonConverterFactory"/> builds converters with
/// <c>MakeGenericType</c>, which needs runtime code generation. Registering the
/// concrete closed generic instead means the AOT compiler can see every
/// instantiation at build time.
/// </remarks>
public static class VerdictJsonAotExtensions
{
    /// <summary>
    /// Registers converters for <see cref="Result{T}"/> and <see cref="Error"/>.
    /// Call once per value type you serialize.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="options">The options to add converters to.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <example>
    /// <code>
    /// var options = new JsonSerializerOptions()
    ///     .AddVerdictConverter&lt;Order&gt;()
    ///     .AddVerdictConverter&lt;Customer&gt;()
    ///     .AddVerdictResultConverter();
    /// </code>
    /// </example>
    public static JsonSerializerOptions AddVerdictConverter<T>(this JsonSerializerOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var errorConverter = new ErrorJsonConverter();
        options.Converters.Add(errorConverter);
        options.Converters.Add(new ResultJsonConverter<T>(errorConverter));
        return options;
    }

    /// <summary>
    /// Registers the converter for the non-generic <see cref="Result"/>.
    /// </summary>
    /// <param name="options">The options to add the converter to.</param>
    /// <returns>The same options, for chaining.</returns>
    public static JsonSerializerOptions AddVerdictResultConverter(this JsonSerializerOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        options.Converters.Add(new ResultNonGenericJsonConverter(new ErrorJsonConverter()));
        return options;
    }
}
