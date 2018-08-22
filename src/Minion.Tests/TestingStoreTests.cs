using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using NSubstitute;
using Xunit;

namespace Minion.Tests
{
    [Trait("Category", "Testing Store Tests")]
    public abstract class TestingStoreTests
    {
        protected IDateService DateService { get; }
        protected ITestingBatchStore Store { get; set; }

        public TestingStoreTests()
        {
            DateService = Substitute.For<IDateService>();
        }

        [Fact(DisplayName = "Get Next Job Due Time Simple")]
        public async Task Get_Next_Job_Due_Time_Simple()
        {
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };

            var jobs = new List<JobDescription>
            {
                job
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.GetNextJobDueTimeAsync();

            Assert.Equal(job.DueTime, r1);

        }

        [Fact(DisplayName = "Get Next Job Due Time Should Only Get Jobs With State Waiting")]
        public async Task Get_Next_Job_Due_Time_Should_Only_Get_Jobs_With_State_Waiting()
        {
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Unknown,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };
            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Running,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };
            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Finished,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };
            var job4 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Error,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };
            var job5 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Halted,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };

            var jobs = new List<JobDescription>
            {
                job,
                job2,
                job3,
                job4,
                job5
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.GetNextJobDueTimeAsync();

            Assert.Null(r1);
        }

        [Fact(DisplayName = "Get Next Job Due Time Should Not Get Jobs With Wait Count Not Equal To Zero")]
        public async Task Get_Next_Job_Due_Time_Should_Not_Get_Jobs_With_Wait_Count_Not_Equal_To_Zero()
        {
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 1,
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };

            var jobs = new List<JobDescription>
            {
                job
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.GetNextJobDueTimeAsync();

            Assert.Null(r1);
        }

        [Fact(DisplayName = "Get Next Job Due Time Should Order By Due Time")]
        public async Task Get_Next_Job_Due_Time_Should_Order_By_Due_Time()
        {
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 6)
            };
            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 3)
            };
            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5)
            };

            var jobs = new List<JobDescription>
            {
                job,
                job2,
                job3
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.GetNextJobDueTimeAsync();

            Assert.Equal(job2.DueTime, r1);
        }
    }
}