using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// A concurrent duplicate request collapser policy that can be applied to synchronous executions.  Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    public partial class RequestCollapserPolicy : Policy, ISyncRequestCollapserPolicy
    {
        internal static IKeyStrategy DefaultKeyStrategy = DuplicateRequestCollapser.DefaultKeyStrategy.Instance;
        internal static ISyncLockProvider GetDefaultLockProvider() => new InstanceScopedLockProvider(); 

        private readonly ConcurrentDictionary<string, Lazy<object>> _collapser = new ConcurrentDictionary<string, Lazy<object>>();
        private readonly IKeyStrategy _keyStrategy;
        private readonly ISyncLockProvider _lockProvider;

        internal RequestCollapserPolicy(IKeyStrategy keyStrategy, ISyncLockProvider lockProvider)
        {
            _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
            _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override TResult Implementation<TResult>(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
            => RequestCollapserEngine.Implementation(
                action,
                context,
                cancellationToken,
                _keyStrategy,
                _collapser,
                _lockProvider);
    }

    /// <summary>
    /// A concurrent duplicate request collapser policy that can be applied to synchronous executions returning a value of type <typeparamref name="TResult"/>.  Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    /// <typeparam name="TResult">The return type of delegates which may be executed through the policy.</typeparam>
    public partial class RequestCollapserPolicy<TResult> : Policy<TResult>, ISyncRequestCollapserPolicy<TResult>
    {
        private readonly ConcurrentDictionary<string, Lazy<object>> _collapser = new ConcurrentDictionary<string, Lazy<object>>();
        private readonly IKeyStrategy _keyStrategy;
        private readonly ISyncLockProvider _lockProvider;

        internal RequestCollapserPolicy(IKeyStrategy keyStrategy, ISyncLockProvider lockProvider)
        {
            _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
            _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override TResult Implementation(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
            => RequestCollapserEngine.Implementation(
                action,
                context,
                cancellationToken,
                _keyStrategy,
                _collapser,
                _lockProvider);
    }
}
