namespace Polly.Contrib.DuplicateRequestCollapser;

internal static class CacheStampedeResilienceOutcomeConstants
{
    /// <summary>
    /// No Key returned from Key Strategy
    /// </summary>
    public const string NoKey = "CacheStampede.NoKey";

    /// <summary>
    /// Exception thrown for callback
    /// </summary>
    public const string OnException = "CacheStampede.Exception";

    /// <summary>
    /// Callback Task was completed
    /// </summary>
    public const string IsCompleted = "CacheStampede.IsCompleted";

    /// <summary>
    /// Callback Task was executed
    /// </summary>
    public const string TaskExecuted = "CacheStampede.TaskExecuted";
}
