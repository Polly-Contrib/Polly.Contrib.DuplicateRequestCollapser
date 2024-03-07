using System;
using System.Threading;

namespace Polly.Contrib.DuplicateRequestCollapser;

/// <summary>
/// Defines operations for locks used by Polly policies.
/// </summary>
public interface ILockProvider
{
    /// <summary>
    /// Waits asynchronously to acquire the lock.
    /// </summary>
    /// <param name="key">A string key being used by the execution.</param>
    /// <param name="context">The Polly execution context consuming this lock.</param>
    /// <param name="cancellationToken">A cancellation token to cancel waiting to acquire the lock.</param>
    /// <param name="continueOnCapturedContext">Whether to continue on a captured context.</param>
    /// <throws>OperationCanceledException, if the passed <paramref name="cancellationToken"/> is signaled before the lock is acquired.</throws>
    IAsyncDisposable AcquireLockAsync(string key, ResilienceContext context, CancellationToken cancellationToken, bool continueOnCapturedContext);
}
