using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class RequestCollapserAsyncSpecs : RequestCollapserSpecsBase
    {
        public RequestCollapserAsyncSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        protected override IsPolicy GetPolicy(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ISyncLockProvider lockProvider = null)
        {
            return useCollapser ?
                AsyncRequestCollapserPolicy.Create(overrideKeyStrategy ?? RequestCollapserPolicy.DefaultKeyStrategy, new AsyncWrapperLockProvider(lockProvider ?? RequestCollapserPolicy.GetDefaultLockProvider()))
                : (IAsyncPolicy)Policy.NoOpAsync();
        }

        protected override Task ExecuteThroughPolicy(IsPolicy policy, Context context, int j, bool gated)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                await ((IAsyncPolicy)policy).ExecuteAsync(ctx =>
                {
                    UnderlyingExpensiveWork(j);
                    return Task.CompletedTask;
                }, context);
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

    }
}
