using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public abstract partial class CacheStampedeResilienceSpecsBase
    {
        private ITestOutputHelper testOutputHelper; 
        
        private const string SharedKey = "SomeSharedKey";

        protected readonly TimeSpan SpinWaitForCohesion = TimeSpan.FromMilliseconds(50);
        protected readonly TimeSpan BlockWaitToCauseBlockingConcurrency = TimeSpan.FromMilliseconds(200); // Consider increasing this TimeSpan if tests fail transiently in slower-running CI environments.

        protected int QueuedExecutions;
        protected int ActualExecutions;

        protected Task[] ConcurrentTasks;
        private ManualResetEventSlim ConcurrentExecutionHoldingGate = new ManualResetEventSlim();

        protected CacheStampedeResilienceSpecsBase(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        protected void UnderlyingExpensiveWork(int i)
        {
            testOutputHelper.WriteLine($"Executed inner work item {i}.");

            Thread.Sleep(BlockWaitToCauseBlockingConcurrency);

            Interlocked.Increment(ref ActualExecutions);
        }

        protected (int actualExecutions, Task[] tasks) Execute_parallel_delegates_through_policy_with_key_strategy(int parallelism, bool useCollapser, bool sameKey, IKeyStrategy overrideKeyStrategy = null)
        {
            ConcurrentTasks = new Task[parallelism];

            testOutputHelper.WriteLine("Queueing work.");
            var contexts = QueueTasks(parallelism, useCollapser, sameKey, overrideKeyStrategy);

            testOutputHelper.WriteLine("Waiting for all queued work to start and reach the holding gate.");
            WaitForAllTasksToBeStarted(parallelism);
            Thread.Sleep(BlockWaitToCauseBlockingConcurrency);

            // Release the parallel contention.
            testOutputHelper.WriteLine("All tasks started. Releasing holding gate.");
            ReleaseHoldingGate();

            // Wait for task completion.
            try
            {
                Task.WaitAll(ConcurrentTasks);
            }
            catch
            {
            }
            testOutputHelper.WriteLine("All tasks completed.");
            for (int i = 0; i < contexts.Length; i++)
            {
                ResilienceContextPool.Shared.Return(contexts[i]);
            }
            // Return results to caller; the caller is responsible for asserting.
            return (ActualExecutions, ConcurrentTasks);
        }

        private ResilienceContext[] QueueTasks(int parallelism, bool useCollapser, bool sameKey, IKeyStrategy overrideKeyStrategy = null)
        {
            ResiliencePipeline policy = GetResiliencePipeline(useCollapser, overrideKeyStrategy);
            ResilienceContext[] resilienceContexts = new ResilienceContext[parallelism];
            for (int i = 0; i < parallelism; i++)
            {
                string key = sameKey ? SharedKey : i.ToString();
                ResilienceContext context = ResilienceContextPool.Shared.Get(key);
                resilienceContexts[i] = context;
                ConcurrentTasks[i] = ExecuteThroughPolicy(policy, context, i, true);
            }
            return resilienceContexts;
        }

        protected abstract ResiliencePipeline GetResiliencePipeline(bool useCollapser, IKeyStrategy overrideKeyStrategy = null, ILockProvider lockProvider = null);

        protected abstract Task ExecuteThroughPolicy(ResiliencePipeline policy, ResilienceContext context, int j, bool gated);

        protected void QueueTaskAtHoldingGate()
        {
            Interlocked.Increment(ref QueuedExecutions);

            // Hold all executions at a ManualResetEventSlim, so that they can all be released on to the policy at concurrently as possible.
            ConcurrentExecutionHoldingGate.WaitHandle.WaitOne();
        }

        private void WaitForAllTasksToBeStarted(int parallelism)
        {
            bool tasksUnstarted;
            do
            {
                testOutputHelper.WriteLine($"Count tasks started = {QueuedExecutions}.");
                tasksUnstarted = QueuedExecutions < parallelism;

                if (tasksUnstarted) { Thread.Sleep(SpinWaitForCohesion); }

            } while (tasksUnstarted);
        }

        private void ReleaseHoldingGate()
        {
            ConcurrentExecutionHoldingGate.Set();
            ConcurrentExecutionHoldingGate.Reset();
        }
    }
}
