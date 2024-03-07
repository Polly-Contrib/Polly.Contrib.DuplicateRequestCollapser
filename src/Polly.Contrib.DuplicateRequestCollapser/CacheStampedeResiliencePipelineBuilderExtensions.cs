using Polly.Contrib.DuplicateRequestCollapser;
using Polly.Contrib.DuplicateRequestCollapser.Utils;

namespace Polly;

/// <summary>
/// Extensions for adding timeout to <see cref="ResiliencePipelineBuilder"/>.
/// </summary>
public static class CacheStampedeResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// A concurrent duplicate request collapser policy. Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>The same builder instance.</returns>
    public static TBuilder AddCacheStampedeResilience<TBuilder>(this TBuilder builder, CacheStampedeResilienceStrategyOptions options)
    where TBuilder : ResiliencePipelineBuilderBase
    {
        Guard.NotNull(builder);
        Guard.NotNull(options);

        builder.AddStrategy(context => new CacheStampedeResilienceStrategy(options, context.Telemetry), options);
        return builder;
    }
}
