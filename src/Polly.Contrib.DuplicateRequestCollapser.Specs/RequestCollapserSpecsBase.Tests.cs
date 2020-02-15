using System;
using System.Threading;
using FluentAssertions;
using Moq;
using Xunit;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public abstract partial class RequestCollapserSpecsBase
    {
        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Should_execute_concurrent_duplicate_task_only_once_when_executed_through_CollapserPolicy(int parallelism)
        {
            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: true)
                .actualExecutions
                .Should().Be(1);
        }

        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Should_execute_concurrent_duplicate_task_multiple_times_when_not_executed_through_CollapserPolicy(int parallelism)
        {
            // This test does not test RequestCollapser policy.
            // It exists to verify the test harness: ie, that _without_ RequestCollapser policy, we do indeed get N separate executions.

            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: false, sameKey: true)
                .actualExecutions
                .Should().Be(parallelism);
        }

        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Should_execute_concurrent_duplicate_task_single_time_when_custom_key_strategy_on_CollapserPolicy_forces_same_key(int parallelism)
        {
            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: false, new CustomKeyStrategy(_ => "ConstantKey"))
                .actualExecutions
                .Should().Be(1);
        }

        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Should_execute_concurrent_non_duplicate_tasks_each_separately_despite_executed_through_CollapserPolicy(int parallelism)
        {
            Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: false)
                .actualExecutions
                .Should().Be(parallelism);
        }

        [Fact]
        public void Should_execute_sequential_duplicate_tasks_each_separately_despite_through_CollapserPolicy()
        {
            // Arrange
            var policy = GetPolicy(useCollapser: true);
            var context = new Context(SharedKey);

            var first = ExecuteThroughPolicy(policy, context, 1, false);
            first.Wait();

            var second = ExecuteThroughPolicy(policy, context, 2, false);
            second.Wait();

            ActualExecutions.Should().Be(2);
        }

        [Fact]
        public void Should_execute_through_CollapserPolicy_using_configured_lock()
        {
            // Arrange
            ISyncLockProvider underlyingLock = new InstanceScopedLockProvider();
            int lockAcquireCount = 0;
            Mock<ISyncLockProvider> lockProviderMock = new Mock<ISyncLockProvider>();
            lockProviderMock
                .Setup(m => m.AcquireLock(It.IsAny<string>(), It.IsAny<Context>(), It.IsAny<CancellationToken>()))
                .Returns<string, Context, CancellationToken>((k, c, ct) =>
                {
                    lockAcquireCount++;
                    return underlyingLock.AcquireLock(k, c, ct);
                });
            
            var policy = GetPolicy(useCollapser: true, lockProvider: lockProviderMock.Object);
            var context = new Context(SharedKey);

            ExecuteThroughPolicy(policy, context, 1, false).Wait();

            lockAcquireCount.Should().Be(2);
        }

        [Fact]
        public void Should_execute_through_CollapserPolicy_using_configured_striped_lock()
        {
            // Arrange
            ISyncLockProvider underlyingLock = new InstanceScopedStripedLockProvider();
            int lockAcquireCount = 0;
            Mock<ISyncLockProvider> lockProviderMock = new Mock<ISyncLockProvider>();
            lockProviderMock
                .Setup(m => m.AcquireLock(It.IsAny<string>(), It.IsAny<Context>(), It.IsAny<CancellationToken>()))
                .Returns<string, Context, CancellationToken>((k, c, ct) =>
                {
                    lockAcquireCount++;
                    return underlyingLock.AcquireLock(k, c, ct);
                });

            var policy = GetPolicy(useCollapser: true, lockProvider: lockProviderMock.Object);
            var context = new Context(SharedKey);

            ExecuteThroughPolicy(policy, context, 1, false).Wait();

            lockAcquireCount.Should().Be(2);
        }

        private class CustomKeyStrategy : IKeyStrategy
        {
            private Func<Context, string> _strategy;

            public CustomKeyStrategy(Func<Context, string> strategy)
            {
                _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            }

            public string GetKey(Context context) => _strategy(context);
        }
    }
}
