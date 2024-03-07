using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.DuplicateRequestCollapser;

/// <summary>
/// Provides a lock scoped to this instance of <see cref="InstanceScopedLockProvider"/>.
/// This is a reference implementation of locking and may have performance issues with high concurrent
/// request counts and high CPU core counts.
/// <br/>
/// Please use <see cref="InstanceScopedStripedLockProvider"/>, which locks per key and has
/// much lower CPU usage, especially as more concurrent requests are made on higher core counts.
/// </summary>
public class InstanceScopedLockProvider : ILockProvider
{
    private readonly object _lockObject = new object();

    /// <inheritdoc/>
    public IAsyncDisposable AcquireLockAsync(string key, ResilienceContext context, CancellationToken cancellationToken, bool continueOnCapturedContext)
    {
        Monitor.Enter(_lockObject);
        return new InstanceScopedLockReleaser(this);
    }

    private void ReleaseLock()
    {
        Monitor.Exit(_lockObject);
    }

    private class InstanceScopedLockReleaser : IAsyncDisposable
    {
        private InstanceScopedLockProvider? _lockProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="provider">Lock provider</param>
        public InstanceScopedLockReleaser(InstanceScopedLockProvider provider)
        {
            _lockProvider = provider;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            InstanceScopedLockProvider? provider = _lockProvider;
            if (provider != null && Interlocked.CompareExchange(ref _lockProvider, null, provider) == provider)
            {
                provider.ReleaseLock();
            }

#if NET6_0_OR_GREATER
            return ValueTask.CompletedTask;
#else
            return new ValueTask();
#endif
        }
    }
}
