using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// Provides an async wrapper over a synchronous <see cref="ISyncLockProvider"/>.
    /// </summary>
    internal class AsyncWrapperLockProvider : IAsyncLockProvider
    {
        private readonly ISyncLockProvider _syncLockProvider;

        /// <summary>
        /// Creates a new instance of an <see cref="AsyncWrapperLockProvider"/>
        /// </summary>
        public AsyncWrapperLockProvider(ISyncLockProvider syncLockProvider)
        {
            _syncLockProvider = syncLockProvider;
        }

        /// <inheritdoc/>
        public IAsyncDisposable AcquireLockAsync(string key, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            return new AsyncWrapperLockReleaser(_syncLockProvider.AcquireLock(key, context, cancellationToken));
        }

        private class AsyncWrapperLockReleaser : IAsyncDisposable
        {
            private IDisposable _syncLockReleaser;

            internal AsyncWrapperLockReleaser(IDisposable syncLockReleaser)
            {
                _syncLockReleaser = syncLockReleaser;
            }

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
            {
                _syncLockReleaser.Dispose();
                return new ValueTask();
            }
        }
    }
}