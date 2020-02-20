using System;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// Provides a lock scoped to a distributed key.
    /// </summary>
    public class StackexchangeRedisDistributedLockProvider : ISyncLockProvider
    {
        private readonly StackexchangeRedisDistributedLockProviderOptions options;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">Options</param>
        public StackexchangeRedisDistributedLockProvider(StackexchangeRedisDistributedLockProviderOptions options)
        {
            this.options = options;
        }

        /// <inheritdoc/>
        public IDisposable AcquireLock(string key, Context context, CancellationToken cancellationToken)
        {
            DistributedLockReleaser distributedLock = new DistributedLockReleaser(key, this);
            bool gotLock = distributedLock.AcquireLock(cancellationToken);
            if (!gotLock)
            {
                throw new OperationCanceledException("Failed to acquire distributed lock for key " + key);
            }
            return distributedLock;
        }

        private class DistributedLockReleaser : IDisposable
        {
            private readonly RedisDistributedCacheLock _distributedLock;

            private StackexchangeRedisDistributedLockProvider _lockProvider;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="key">Key</param>
            /// <param name="provider">Lock provider</param>
            public DistributedLockReleaser(string key, StackexchangeRedisDistributedLockProvider provider)
            {
                _lockProvider = provider;
                _distributedLock = new RedisDistributedCacheLock(provider.options.Connection, key, provider.options.Timeout, provider.options.Retry);
            }

            /// <summary>
            /// Acquire the distributed lock
            /// </summary>
            /// <param name="cancelToken">Cancel token</param>
            /// <returns>True if lock acquired, false otherwise</returns>
            public bool AcquireLock(CancellationToken cancelToken)
            {
                return _distributedLock.AcquireLock(cancelToken);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                StackexchangeRedisDistributedLockProvider provider = _lockProvider;
                if (provider != null && Interlocked.CompareExchange(ref _lockProvider, null, provider) == provider)
                {
                    _distributedLock.Dispose();
                }
            }
        }

        private class RedisDistributedCacheLock : IDisposable
        {
            private readonly ConnectionMultiplexer connection;
            private readonly string lockKey;
            private readonly TimeSpan timeout;
            private readonly TimeSpan retry;
            private readonly string lockValue;

            private int hasLock;

            public RedisDistributedCacheLock(ConnectionMultiplexer connection, string key, TimeSpan timeout, TimeSpan retry)
            {
                this.connection = connection;
                this.lockKey = key;
                this.timeout = timeout;
                this.retry = retry;
                lockValue = Environment.MachineName;
            }

            public bool AcquireLock(CancellationToken cancelToken)
            {
                const string script = "local f,k,v f=redis.call k=KEYS[1] v=ARGV[1] if f('get',k) then return 0 end f('set',k,v) f('expire',k,ARGV[2]) return 1";
                RedisKey[] keys = new RedisKey[] { lockKey };
                RedisValue[] values = new RedisValue[] { lockValue, (int)timeout.TotalSeconds };
                DateTime startDateTime = DateTime.UtcNow;
                while (!cancelToken.IsCancellationRequested && (DateTime.UtcNow - startDateTime) < timeout)
                {
                    try
                    {
                        hasLock = (int)connection.GetDatabase().ScriptEvaluate(script, keys, values);
                        if (hasLock == 1)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // eat any and all exception
                    }
                    Thread.Sleep(retry);
                }
                return false;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (hasLock == 1)
                {
                    // get key out of there, we no longer need the lock
                    connection.GetDatabase().KeyExpire(lockKey, TimeSpan.Zero);
                    hasLock = 0;
                }
            }
        }
    }

    /// <summary>
    /// Options for StackexchangeRedisDistributedLockProvider
    /// </summary>
    public class StackexchangeRedisDistributedLockProviderOptions
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection</param>
        /// <param name="timeout">Wait this amount of time to get the lock</param>
        /// <param name="retry">After a failed lock acquire attempt, wait this amount of time before trying again</param>
        public StackexchangeRedisDistributedLockProviderOptions(ConnectionMultiplexer connection, TimeSpan timeout, TimeSpan retry)
        {
            Connection = connection;
            Timeout = timeout;
            Retry = retry;
        }

        /// <summary>
        /// Connection
        /// </summary>
        public ConnectionMultiplexer Connection { get; }

        /// <summary>
        /// Timeout, give up trying to get the distributed lock
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Retry wait time after each failed distributed lock attempt
        /// </summary>
        public TimeSpan Retry { get; }
    }
}