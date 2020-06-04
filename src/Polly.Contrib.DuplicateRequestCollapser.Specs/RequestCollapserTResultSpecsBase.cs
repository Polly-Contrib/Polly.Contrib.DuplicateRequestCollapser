﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Polly.Contrib.DuplicateRequestCollapser.Specs
{
    public abstract class RequestCollapserTResultSpecsBase : RequestCollapserSpecsBase
    {
        private Random _rng = new Random();

        protected RequestCollapserTResultSpecsBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private protected Func<ResultClass> ResultFactory = () => new ResultClass(ResultPrimitive.Good);

        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Executing_concurrent_duplicate_task_through_CollapserPolicy_should_execute_only_once_and_return_same_single_result_instance(int parallelism)
        {
            (int actualInvocations, Task[] tasks) = Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: true);

            actualInvocations.Should().Be(1);

            // All executions should have been handled the same single result instance.
            ResultClass first = ((Task<ResultClass>)tasks[0]).Result;

            tasks.All(t => Object.ReferenceEquals(((Task<ResultClass>)t).Result, first)).Should().BeTrue();
        }

        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Executing_concurrent_duplicate_task_not_through_CollapserPolicy_should_execute_multiple_and_return_separate_result_instances(int parallelism)
        {
            // This test does not test RequestCollapser policy.
            // It exists to verify the test harness: ie, that _without_ RequestCollapser policy, we do indeed get N separate executions giving separate results.

            (int actualInvocations, Task[] tasks) = Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: false, sameKey: true);

            actualInvocations.Should().Be(parallelism);

            // All executions should have been handled the same single result instance.
            tasks.Select(t => ((Task<ResultClass>)t).Result).Distinct().Count().Should().Be(parallelism);
        }

        [Theory]
        [ClassData(typeof(RequestCollapserTestParallelisms))]
        public void Executing_concurrent_duplicate_faulted_task_through_CollapserPolicy_should_execute_only_once_and_return_same_single_result_instance(int parallelism)
        {
            ResultFactory = () => throw new Exception(_rng.Next().ToString());
            (int actualInvocations, Task[] tasks) = Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: true);
            actualInvocations.Should().Be(1);
            tasks.First().IsFaulted.Should().BeTrue();

            Exception first = tasks.First().Exception.InnerException;
            // All executions should have been handed the same single result instance.
            tasks.Select(x => x.Exception.InnerException).ShouldAllBeEquivalentTo(first);

            (actualInvocations, tasks) = Execute_parallel_delegates_through_policy_with_key_strategy(parallelism, useCollapser: true, sameKey: true);
            actualInvocations.Should().Be(2);
            tasks.First().IsFaulted.Should().BeTrue();

            Exception second = tasks.First().Exception.InnerException;
            // The result of the second batch should not be the same as the first batch.
            second.Message.Should().NotBe(first.Message);
            tasks.Select(x => x.Exception.InnerException).ShouldAllBeEquivalentTo(second);
        }
    }
}
