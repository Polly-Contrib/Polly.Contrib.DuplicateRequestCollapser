using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class CacheStampedeResilienceTResultSpecs : CacheStampedeResilienceTResultSpecsBase
    {
        public CacheStampedeResilienceTResultSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

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
            return Task.Factory.StartNew(() =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                return policy.Execute(ctx =>
                {
                    UnderlyingExpensiveWork(j);
                    return ResultFactory();
                }, context);
            }, TaskCreationOptions.LongRunning);
        }

    }
}
