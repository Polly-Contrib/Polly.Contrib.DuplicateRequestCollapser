using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.DuplicateRequestCollapser
{
    /// <summary>
    /// A concurrent duplicate request collapser policy that can be applied to asynchronous executions.  Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    public partial class AsyncRequestCollapserPolicy : AsyncPolicy, IAsyncRequestCollapserPolicy
    {
        internal static IAsyncLockProvider GetDefaultLockProvider() => new AsyncWrapperLockProvider(new InstanceScopedLockProvider());

        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _collapser = new ConcurrentDictionary<string, Lazy<Task<object>>>();
        private readonly IKeyStrategy _keyStrategy;
        private readonly IAsyncLockProvider _lockProvider;

        internal AsyncRequestCollapserPolicy(IKeyStrategy keyStrategy, IAsyncLockProvider lockProvider)
        {
            _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
            _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override Task<TResult> ImplementationAsync<TResult>( Func<Context, CancellationToken,Task<TResult>> action, Context context, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
            => RequestCollapserEngineAsync.ImplementationAsync(
                action,
                context,
                cancellationToken,
                continueOnCapturedContext,
                _keyStrategy,
                _collapser,
                _lockProvider);
    }

    /// <summary>
    /// A concurrent duplicate request collapser policy that can be applied to asynchronous executions returning a value of type <typeparamref name="TResult"/>.  Code executed through the policy is executed as if no policy was applied.
    /// </summary>
    /// <typeparam name="TResult">The return type of delegates which may be executed through the policy.</typeparam>
    public partial class AsyncRequestCollapserPolicy<TResult> : AsyncPolicy<TResult>, IAsyncRequestCollapserPolicy<TResult>
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _collapser = new ConcurrentDictionary<string, Lazy<Task<object>>>();
        private readonly IKeyStrategy _keyStrategy;
        private readonly IAsyncLockProvider _lockProvider;

        internal AsyncRequestCollapserPolicy(IKeyStrategy keyStrategy, IAsyncLockProvider lockProvider)
        {
            _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
            _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override Task<TResult> ImplementationAsync(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
            => RequestCollapserEngineAsync.ImplementationAsync(
                action,
                context,
                cancellationToken,
                continueOnCapturedContext,
                _keyStrategy,
                _collapser,
                _lockProvider);
    }
}