using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using NSubstitute;
using Xunit;

namespace Minion.Tests
{
    [Trait("Category", "Store Tests")]
    public abstract class StoreTests
    {
        protected IDateService DateService { get; }
        protected IBatchStore Store { get; set; }

        protected StoreTests()
        {
            DateService = Substitute.For<IDateService>();
        }

        [Fact(DisplayName = "Init Can Run Twice")]
        public async Task Init_Can_Run_Twice()
        {
            await Store.InitAsync();
            await Store.InitAsync();
        }

        [Fact(DisplayName = "Acquire Job In Simple Sequence")]
        public async Task Acquire_Job_In_Simple_Sequence()
        {
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                PrevId = job1.Id,
                WaitCount = 1,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                PrevId = job2.Id,
                WaitCount = 1,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1,
                job2,
                job3
            };

            await Store.AddJobsAsync(jobs);

            //Fetch first item
            var r1 = await Store.AcquireJobAsync();

            Assert.Equal(job1.Id, r1.Id);

            //Fetching should return null
            var r2 = await Store.AcquireJobAsync();

            Assert.Null(r2);

            await Store.ReleaseJobAsync(r1.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            //Fetch second item
            var r3 = await Store.AcquireJobAsync();

            Assert.Equal(job2.Id, r3.Id);

            //Fetching should return null
            var r4 = await Store.AcquireJobAsync();

            Assert.Null(r4);

            await Store.ReleaseJobAsync(r3.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            //Fetch third item
            var r5 = await Store.AcquireJobAsync();

            Assert.Equal(job3.Id, r5.Id);

            //Fetching should return null
            var r6 = await Store.AcquireJobAsync();

            Assert.Null(r6);

            await Store.ReleaseJobAsync(r5.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            //Should be no items left
            var r7 = await Store.AcquireJobAsync();

            Assert.Null(r7);

        }

        [Fact(DisplayName = "Acquire Job In Simple Set")]
        public async Task Acquire_Job_In_Simple_Set()
        {
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1,
                job2,
                job3
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.AcquireJobAsync();
            var r2 = await Store.AcquireJobAsync();
            var r3 = await Store.AcquireJobAsync();

            Assert.Equal(job1.Id, r1.Id);
            Assert.Equal(job2.Id, r2.Id);
            Assert.Equal(job3.Id, r3.Id);

            var r4 = await Store.AcquireJobAsync();

            Assert.Null(r4);

        }

        [Fact(DisplayName = "Acquire Job Should Update State")]
        public async Task Acquire_Job_Should_Update_State()
        {
            //Arrange
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1
            };

            var date = new DateTime(2017, 1, 2, 3, 4, 5);

            DateService.GetNow().Returns(date);

            //Act
            await Store.AddJobsAsync(jobs);

            var result = await Store.AcquireJobAsync();

            //Assert
            Assert.Equal(ExecutionState.Running, result.State);
            Assert.Equal(date, result.UpdatedTime);
        }

        [Fact(DisplayName = "Acquire Job Sequence In Set")]
        public async Task Acquire_Job_Sequence_In_Set()
        {
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                PrevId = job2.Id,
                WaitCount = 1,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var job4 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1,
                job2,
                job3,
                job4
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.AcquireJobAsync();
            var r2 = await Store.AcquireJobAsync();
            var r3 = await Store.AcquireJobAsync();
            var r4 = await Store.AcquireJobAsync();

            Assert.Equal(job1.Id, r1.Id);
            Assert.Equal(job2.Id, r2.Id);
            Assert.Equal(job4.Id, r3.Id);
            Assert.Null(r4);

            await Store.ReleaseJobAsync(r2.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            var r5 = await Store.AcquireJobAsync();

            Assert.Equal(job3.Id, r5.Id);
        }

        [Fact(DisplayName = "Acquire Job Set In Sequence")]
        public async Task Acquire_Job_Set_In_Sequence()
        {

            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                PrevId = job1.Id,
                WaitCount = 1,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                PrevId = job1.Id,
                WaitCount = 1,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var job4 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 2,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            job2.NextId = job4.Id;
            job3.NextId = job4.Id;

            var jobs = new List<JobDescription>
            {
                job1,
                job2,
                job3,
                job4
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.AcquireJobAsync();
            var r2 = await Store.AcquireJobAsync();

            Assert.Equal(job1.Id, r1.Id);
            Assert.Null(r2);

            await Store.ReleaseJobAsync(r1.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            var r3 = await Store.AcquireJobAsync();
            var r4 = await Store.AcquireJobAsync();
            var r5 = await Store.AcquireJobAsync();

            Assert.Equal(job2.Id, r3.Id);
            Assert.Equal(job3.Id, r4.Id);
            Assert.Null(r5);

            await Store.ReleaseJobAsync(r3.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            var r6 = await Store.AcquireJobAsync();
            Assert.Null(r6);

            await Store.ReleaseJobAsync(r4.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            var r7 = await Store.AcquireJobAsync();
            var r8 = await Store.AcquireJobAsync();
            Assert.Equal(job4.Id, r7.Id);
            Assert.Null(r8);

            await Store.ReleaseJobAsync(r7.Id, new JobResult
            {
                State = ExecutionState.Finished
            });

            var r9 = await Store.AcquireJobAsync();
            Assert.Null(r9);
        }

        [Fact(DisplayName = "Acquire Job Should Get Job With Highest Priority First")]
        public async Task Acquire_Job_Should_Get_Job_With_Highest_Priority_First()
        {
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                Priority = 0,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                Priority = 200,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                Priority = 1000,
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job4 = new JobDescription
            {
                Id = Guid.NewGuid(),
                WaitCount = 0,
                Priority = 100,
                State = ExecutionState.Waiting,
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1,
                job2,
                job3,
                job4
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.AcquireJobAsync();
            var r2 = await Store.AcquireJobAsync();
            var r3 = await Store.AcquireJobAsync();
            var r4 = await Store.AcquireJobAsync();
            var r5 = await Store.AcquireJobAsync();

            Assert.Equal(job3.Id, r1.Id);
            Assert.Equal(job2.Id, r2.Id);
            Assert.Equal(job4.Id, r3.Id);
            Assert.Equal(job1.Id, r4.Id);
            Assert.Null(r5);
        }

        [Fact(DisplayName = "Acquire Job With Input")]
        public async Task Acquire_Job_With_Input()
        {
            //Arrange
            var data = new TestData
            {
                Text = "text",
                Number = 123
            };

            var date = new DateTime(2018, 1, 2, 3, 4, 5);

            DateService.GetNow().Returns(date);
            
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                Input = new JobInputDescription
                {
                    InputData = data,
                    Type = typeof(TestData).AssemblyQualifiedName
                },
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 5),
                Type = "type",
                Priority = 100,
                PrevId = Guid.NewGuid(),
                NextId = Guid.NewGuid(),
                BatchId = Guid.NewGuid(),
                CreatedTime = new DateTime(2016, 1, 2, 3, 4, 5),
                WaitCount = 0,
                StatusInfo = "status"
            };

            var jobs = new List<JobDescription>
            {
                job1
            };

            //Act
            await Store.AddJobsAsync(jobs);

            var result = await Store.AcquireJobAsync();

            //Assert
            var inputResult = (TestData)result.Input.InputData;

            Assert.Equal(data.Text, inputResult.Text);
            Assert.Equal(data.Number, inputResult.Number);

            Assert.Equal(job1.Id, result.Id);
            Assert.Equal(ExecutionState.Running, result.State);
            Assert.Equal(job1.DueTime, result.DueTime);
            Assert.Equal(job1.Type, result.Type);
            Assert.Equal(job1.Priority, result.Priority);
            Assert.Equal(job1.PrevId, result.PrevId);
            Assert.Equal(job1.NextId, result.NextId);
            Assert.Equal(job1.BatchId, result.BatchId);
            Assert.Equal(job1.CreatedTime, result.CreatedTime);
            Assert.Equal(job1.WaitCount, result.WaitCount);
            Assert.Equal(job1.StatusInfo, result.StatusInfo);
            Assert.Equal(date, result.UpdatedTime);
        }

        [Fact(DisplayName = "Acquire Job Where Due Time Is Greater Than Now Should Return Null")]
        public async Task Acquire_Job_Where_Due_Time_Is_Greater_Than_Now_Should_Return_Null()
        {
            var date = new DateTime(2017, 1, 2, 3, 4, 5);

            DateService.GetNow().Returns(date);

            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Waiting,
                DueTime = new DateTime(2017, 1, 2, 3, 4, 6),
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.AcquireJobAsync();

            Assert.Null(r1);


            date = date.AddSeconds(1);

            DateService.GetNow().Returns(date);

            var r2 = await Store.AcquireJobAsync();

            Assert.Equal(job1.Id, r2.Id);
        }

        [Fact(DisplayName = "Acquire Job Should Only Fetch Jobs With State Waiting")]
        public async Task Acquire_Job_Should_Only_Fetch_Jobs_With_State_Waiting()
        {
            var job1 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Unknown,
                Type = "type"
            };
            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Waiting,
                Type = "type"
            };
            var job3 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Running,
                Type = "type"
            };
            var job4 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Finished,
                Type = "type"
            };
            var job5 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Error,
                Type = "type"
            };
            var job6 = new JobDescription
            {
                Id = Guid.NewGuid(),
                State = ExecutionState.Halted,
                Type = "type"
            };

            var jobs = new List<JobDescription>
            {
                job1,
                job2,
                job3,
                job4,
                job5,
                job6,
            };

            await Store.AddJobsAsync(jobs);

            var r1 = await Store.AcquireJobAsync();
            var r2 = await Store.AcquireJobAsync();

            Assert.Equal(job2.Id, r1.Id);
            Assert.Null(r2);
        }

        public class TestData
        {
            public string Text { get; set; }
            public int Number { get; set; }
        }
    }
}