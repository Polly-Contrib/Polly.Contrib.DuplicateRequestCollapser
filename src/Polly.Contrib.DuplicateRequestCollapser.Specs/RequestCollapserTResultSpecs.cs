using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public class RequestCollapserTResultSpecs : RequestCollapserTResultSpecsBase
    {
        public RequestCollapserTResultSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        protected override IsPolicy GetPolicy(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ISyncLockProvider lockProvider = null)
        {
            return useCollapser ?
                RequestCollapserPolicy<ResultClass>.Create(overrideKeyStrategy ?? RequestCollapserPolicy.DefaultKeyStrategy, lockProvider ?? RequestCollapserPolicy.GetDefaultLockProvider())
                : (ISyncPolicy<ResultClass>)Policy.NoOp<ResultClass>();
        }

        protected override Task ExecuteThroughPolicy(IsPolicy policy, Context context, int j, bool gated)
        {
            return Task.Factory.StartNew(() =>
            {
                if (gated) { QueueTaskAtHoldingGate(); }

                return ((ISyncPolicy<ResultClass>)policy).Execute(ctx =>
                {
                    UnderlyingExpensiveWork(j);
                    return ResultFactory();
                }, context);
            }, TaskCreationOptions.LongRunning);
        }

    }
}
