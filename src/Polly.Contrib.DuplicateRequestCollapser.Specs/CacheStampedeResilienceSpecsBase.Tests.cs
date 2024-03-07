using System;
using System.Threading;
using FluentAssertions;
using Xunit;
using NSubstitute;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public abstract partial class CacheStampedeResilienceSpecsBase
    {
        [Theory]
        [ClassData(typeof(CacheStampedeResilienceTestParallelisms))]
        public void Should_execute_concurrent_duplicate_task_only_once_when_executed_through_CollapserPolicy(int parallelism)
        {
            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: true)
                .actualExecutions
                .Should().Be(1);
        }

        [Theory]
        [ClassData(typeof(CacheStampedeResilienceTestParallelisms))]
        public void Should_execute_concurrent_duplicate_task_multiple_times_when_not_executed_through_CollapserPolicy(int parallelism)
        {
            // This test does not test RequestCollapser policy.
            // It exists to verify the test harness: ie, that _without_ RequestCollapser policy, we do indeed get N separate executions.

            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: false, sameKey: true)
                .actualExecutions
                .Should().Be(parallelism);
        }

        [Theory]
        [ClassData(typeof(CacheStampedeResilienceTestParallelisms))]
        public void Should_execute_concurrent_duplicate_task_single_time_when_custom_key_strategy_on_CollapserPolicy_forces_same_key(int parallelism)
        {
            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: false, new CustomKeyStrategy(_ => "ConstantKey"))
                .actualExecutions
                .Should().Be(1);
        }

        [Theory]
        [ClassData(typeof(CacheStampedeResilienceTestParallelisms))]
        public void Should_execute_concurrent_non_duplicate_tasks_each_separately_despite_executed_through_CollapserPolicy(int parallelism)
        {
            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: false)
                .actualExecutions
                .Should().Be(parallelism);
        }

        [Fact]
        public async System.Threading.Tasks.Task Should_execute_sequential_duplicate_tasks_each_separately_despite_through_CollapserPolicy()
        {
            // Arrange
            var policy = GetResiliencePipeline(useCollapser: true);
            var context = ResilienceContextPool.Shared.Get(SharedKey);

            var first = ExecuteThroughPolicy(policy, context, 1, false);
            await first;
            ResilienceContextPool.Shared.Return(context);

            context = ResilienceContextPool.Shared.Get(SharedKey);
            var second = ExecuteThroughPolicy(policy, context, 2, false);
            await second;
            ResilienceContextPool.Shared.Return(context);

            ActualExecutions.Should().Be(2);
        }

        [Fact]
        public async System.Threading.Tasks.Task Should_execute_through_CollapserPolicy_using_configured_lock()
        {
            // Arrange
            ILockProvider underlyingLock = new InstanceScopedLockProvider();
            int lockAcquireCount = 0;
            var lockProviderMock = Substitute.For<ILockProvider>();
            lockProviderMock.AcquireLockAsync(Arg.Any<string>(), Arg.Any<ResilienceContext>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns(x=>
                {
                    lockAcquireCount++;
                    return underlyingLock.AcquireLockAsync(x.ArgAt<string>(0), x.ArgAt<ResilienceContext>(1), x.ArgAt<CancellationToken>(2), x.ArgAt<bool>(3));
                });
            
            var policy = GetResiliencePipeline(useCollapser: true, lockProvider: lockProviderMock);
            var context = ResilienceContextPool.Shared.Get(SharedKey);

            await ExecuteThroughPolicy(policy, context, 1, false);
            ResilienceContextPool.Shared.Return(context);
            lockAcquireCount.Should().Be(2);
        }

        [Fact]
        public async System.Threading.Tasks.Task Should_execute_through_CollapserPolicy_using_configured_striped_lock()
        {
            // Arrange
            ILockProvider underlyingLock = new InstanceScopedStripedLockProvider();
            int lockAcquireCount = 0;
            var lockProviderMock = Substitute.For<ILockProvider>();
            lockProviderMock.AcquireLockAsync(Arg.Any<string>(), Arg.Any<ResilienceContext>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns(x =>
                {
                    lockAcquireCount++;
                    return underlyingLock.AcquireLockAsync(x.ArgAt<string>(0), x.ArgAt<ResilienceContext>(1), x.ArgAt<CancellationToken>(2), x.ArgAt<bool>(3));
                });

            var policy = GetResiliencePipeline(useCollapser: true, lockProvider: lockProviderMock);
            var context = ResilienceContextPool.Shared.Get(SharedKey);

            await ExecuteThroughPolicy(policy, context, 1, false);
            //ResilienceContextPool.Shared.Return(context);

            lockAcquireCount.Should().Be(2);
        }

        private class CustomKeyStrategy : IKeyStrategy
        {
            private Func<ResilienceContext, string> _strategy;

            public CustomKeyStrategy(Func<ResilienceContext, string> strategy)
            {
                _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            }

            public string GetKey(ResilienceContext context) => _strategy(context);
        }
    }
}
