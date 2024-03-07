using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class CacheStampedeResilienceAsyncSpecs : CacheStampedeResilienceSpecsBase
    {
        public CacheStampedeResilienceAsyncSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        protected override ResiliencePipeline GetResiliencePipeline(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ILockProvider lockProvider = null)
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
        }

        protected override Task ExecuteThroughPolicy(ResiliencePipeline policy, ResilienceContext context, int j, bool gated)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                await ((ResiliencePipeline)policy).ExecuteAsync(ctx =>
                {
                    UnderlyingExpensiveWork(j);

#if NET6_0_OR_GREATER
                    return ValueTask.CompletedTask;
#else
                    return new ValueTask();
#endif
                }, context);
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

    }
}
