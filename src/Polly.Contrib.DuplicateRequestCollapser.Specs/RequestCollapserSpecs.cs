using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class RequestCollapserSpecs : RequestCollapserSpecsBase
    {
        public RequestCollapserSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        protected override IsPolicy GetPolicy(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ISyncLockProvider lockProvider = null)
        {
            return useCollapser ?
                RequestCollapserPolicy.Create(overrideKeyStrategy ?? RequestCollapserPolicy.DefaultKeyStrategy, lockProvider ?? RequestCollapserPolicy.GetDefaultLockProvider())
                : (ISyncPolicy)Policy.NoOp();
        }

        protected override Task ExecuteThroughPolicy(IsPolicy policy, Context context, int j, bool gated)
        {
            return Task.Factory.StartNew(() =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                ((ISyncPolicy)policy).Execute(ctx => UnderlyingExpensiveWork(j), context);
            }, TaskCreationOptions.LongRunning);
        }

    }
}
