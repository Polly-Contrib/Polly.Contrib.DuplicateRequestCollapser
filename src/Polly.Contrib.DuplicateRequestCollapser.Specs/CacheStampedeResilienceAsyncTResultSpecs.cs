using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class CacheStampedeResilienceAsyncTResultSpecs : CacheStampedeResilienceTResultSpecsBase
    {
        public CacheStampedeResilienceAsyncTResultSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private ConcurrentDictionary<(bool, IKeyStrategy, ILockProvider), ResiliencePipeline> PolicyCache = new ConcurrentDictionary<(bool, IKeyStrategy, ILockProvider), ResiliencePipeline>();

        protected override ResiliencePipeline GetResiliencePipeline(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ILockProvider lockProvider = null)
        {
            return PolicyCache.GetOrAdd((useCollapser, overrideKeyStrategy, lockProvider), _ =>
            {
                var options = new CacheStampedeResilienceStrategyOptions();
                options.KeyStrategy = overrideKeyStrategy ?? options.KeyStrategy;
                options.LockProvider = lockProvider ?? options.LockProvider;
                return useCollapser ?
                    new ResiliencePipelineBuilder()
                       .AddCacheStampedeResilience(options)
                       .Build()
                    : new ResiliencePipelineBuilder()
                       .Build();
            });
        }

        protected override Task ExecuteThroughPolicy(ResiliencePipeline policy, ResilienceContext context, int j, bool gated)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                return await policy.ExecuteAsync(ctx =>
                {
                    UnderlyingExpensiveWork(j);

#if NET6_0_OR_GREATER
                    return ValueTask.FromResult(ResultFactory());
#else
                    return new ValueTask<ResultClass>(ResultFactory());
#endif
                }, context);
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

    }
}
