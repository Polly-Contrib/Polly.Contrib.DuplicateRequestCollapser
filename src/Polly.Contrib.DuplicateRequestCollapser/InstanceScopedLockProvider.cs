using System;
using System.Threading;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// Provides a lock scoped to this instance of <see cref="InstanceScopedLockProvider"/>.
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

            internal InstanceScopedLockReleaser(InstanceScopedLockProvider provider)
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