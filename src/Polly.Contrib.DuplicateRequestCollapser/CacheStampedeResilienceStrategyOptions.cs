namespace Polly.Contrib.DuplicateRequestCollapser;

/// <summary>
/// Represents the options for the Cache Stampede Resilience Strategy
/// </summary>
public class CacheStampedeResilienceStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// A strategy for choosing a key on which to consider requests duplicates.
    /// </summary>
    public IKeyStrategy KeyStrategy { get; set; } = new DefaultKeyStrategy();

    /// <summary>
    /// A provider for locks used by the resilience strategy.
    /// </summary>
    public ILockProvider LockProvider { get; set; } = new InstanceScopedStripedLockProvider();
}
