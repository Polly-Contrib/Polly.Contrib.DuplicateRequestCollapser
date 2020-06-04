using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    internal static class RequestCollapserEngineAsync
    {
        internal static async Task<TResult> ImplementationAsync<TResult>(
            Func<Context, CancellationToken, Task<TResult>> action,
            Context context,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext,
            IKeyStrategy keyStrategy,
            ConcurrentDictionary<string, Lazy<Task<object>>> collapser, 
            IAsyncLockProvider lockProvider)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string key = keyStrategy.GetKey(context);

            // Fast-path if no key specified on Context (similar to CachePolicy).
            if (key == null)
            {
                return await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }

            Lazy<Task<object>> lazy;

            await using (lockProvider.AcquireLockAsync(key, context, cancellationToken, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext))
            {
                lazy = collapser.GetOrAdd(key, new Lazy<Task<object>>(async () => await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext), LazyThreadSafetyMode.ExecutionAndPublication)); // Note: per documentation, LazyThreadSafetyMode.ExecutionAndPublication guarantees single execution, but means the executed code must not lock, as this risks deadlocks.  We should document.
            }

            try
            {
                return (TResult)await lazy.Value.ConfigureAwait(continueOnCapturedContext);
            }
            finally
            {
                // As soon as the lazy has returned a result to one thread, the concurrent request set is over, so we evict the lazy from the ConcurrentDictionary.
                // We need to evict within a lock, to be sure we are not, due to potential race with new threads populating, evicting a different lazy created by a different thread.
                // To reduce lock contention, first check outside the lock whether we still need to remove it (we will double-check inside the lock).
                if (collapser.TryGetValue(key, out Lazy<Task<object>> currentValue))
                {
                    if (currentValue == lazy)
                    {
                        await using (lockProvider.AcquireLockAsync(key, context, cancellationToken, continueOnCapturedContext)
                            .ConfigureAwait(continueOnCapturedContext))
                        {
                            // Double-check that there has not been a race which updated the dictionary with a new value.
                            if (collapser.TryGetValue(key, out Lazy<Task<object>> valueWithinLock))
                            {
                                if (valueWithinLock == lazy)
                                {
                                    collapser.TryRemove(key, out _);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}