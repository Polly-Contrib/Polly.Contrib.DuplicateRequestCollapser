using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class RequestCollapserAsyncTResultSpecs : RequestCollapserTResultSpecsBase
    {
        public RequestCollapserAsyncTResultSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private ConcurrentDictionary<(bool, IKeyStrategy, ISyncLockProvider), IsPolicy> PolicyCache = new ConcurrentDictionary<(bool, IKeyStrategy, ISyncLockProvider), IsPolicy>();

        protected override IsPolicy GetPolicy(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ISyncLockProvider lockProvider = null)
        {
            return PolicyCache.GetOrAdd((useCollapser, overrideKeyStrategy, lockProvider), _ =>
            {
                return useCollapser ?
                AsyncRequestCollapserPolicy<ResultClass>.Create(overrideKeyStrategy ?? RequestCollapserPolicy.DefaultKeyStrategy, new AsyncWrapperLockProvider(lockProvider ?? RequestCollapserPolicy.GetDefaultLockProvider()))
                : (IAsyncPolicy<ResultClass>)Policy.NoOpAsync<ResultClass>();
            });
        }

        protected override Task ExecuteThroughPolicy(IsPolicy policy, Context context, int j, bool gated)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                return await ((IAsyncPolicy<ResultClass>)policy).ExecuteAsync(ctx =>
                {
                    UnderlyingExpensiveWork(j);
                    return Task.FromResult(ResultFactory());
                }, context);
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

    }
}
