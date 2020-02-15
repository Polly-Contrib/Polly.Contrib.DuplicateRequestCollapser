using System;
using System.Threading;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// Provides a lock scoped to this instance of <see cref="InstanceScopedLockProvider"/>.
    /// This is a reference implementation of locking and may have performance issues with high concurrent
    /// request counts and high CPU core counts.
    /// <br/>
    /// Please use <see cref="InstanceScopedStripedLockProvider"/>, which locks per key and has
    /// much lower CPU usage, especially as more concurrent requests are made on higher core counts.
    /// </summary>
    public class InstanceScopedLockProvider : ISyncLockProvider
    {
        private readonly object _lockObject = new object();

        /// <inheritdoc/>
        public IDisposable AcquireLock(string key, Context context, CancellationToken cancellationToken)
        {
            Monitor.Enter(_lockObject);
            return new InstanceScopedLockReleaser(this);
        }

        private void ReleaseLock()
        {
            Monitor.Exit(_lockObject);
        }

        private class InstanceScopedLockReleaser : IDisposable
        {
            private InstanceScopedLockProvider _lockProvider;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="provider">Lock provider</param>
            public InstanceScopedLockReleaser(InstanceScopedLockProvider provider)
            {
                _lockProvider = provider;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var provider = _lockProvider;
                if (provider is null) { return; }

                if (Interlocked.CompareExchange(ref _lockProvider, null, provider) == provider)
                {
                    provider.ReleaseLock();
                }
            }
        }
    }
}