using System;
using System.Threading.Tasks;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using NSubstitute;
using Xunit;

namespace Minion.Tests
{
    [Trait("Category", "Testing Batch Eninge Tests")]
    public class TestingBatchEngineTests
    {
        private readonly ITestingBatchStore _testingBatchStore;
        private readonly IDateSimulationService _dateSimulationService;
        private readonly TestingBatchEngine _engine;

        public TestingBatchEngineTests()
        {
            _testingBatchStore = Substitute.For<ITestingBatchStore>();
            _dateSimulationService = Substitute.For<IDateSimulationService>();
            var dependencyResolver = Substitute.For<IDependencyResolver>();

            _engine = new TestingBatchEngine(_testingBatchStore, _dateSimulationService, dependencyResolver);
        }

        [Fact(DisplayName = "Advance To Date Less Than Now Should Throw")]
        public async Task Advance_To_Date_Less_Than_Now_Should_Throw()
        {
            //Arrange
            var date = new DateTime(2017, 1, 2, 3, 4, 5);
            var now = new DateTime(2017, 1, 2, 3, 4, 6);

            _dateSimulationService.GetNow().Returns(now);


            //Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _engine.AdvanceToDateAsync(date));

            //Assert
            Assert.Equal("Cannot run to a date prior to what is currently now in the date simulation service.", ex.Message);
        }

        [Fact(DisplayName = "Skip To Date Less Than Now Should Throw")]
        public async Task Skip_To_Date_Less_Than_Now_Should_Throw()
        {
            //Arrange
            var date = new DateTime(2017, 1, 2, 3, 4, 5);
            var now = new DateTime(2017, 1, 2, 3, 4, 6);

            _dateSimulationService.GetNow().Returns(now);

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _engine.SkipToDate(date));

            //Assert
            Assert.Equal("Cannot go backwards through time.", ex.Message);
        }

        [Fact(DisplayName = "Advance To Date Single Job")]
        public async Task Advance_To_Date_Single_Job()
        {
            //Arrange
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName,
            };

            var now = new DateTime(2000, 1, 1);
            var to = new DateTime(2019, 2, 2);

            _dateSimulationService.GetNow().Returns(now, now, now, to);

            _testingBatchStore.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            //Act
            await _engine.AdvanceToDateAsync(to);

            //Assert
            _dateSimulationService.Received(4).GetNow();
            _dateSimulationService.Received(1).SetNow(to);
            await _testingBatchStore.Received(1).GetNextJobDueTimeAsync();
            await _testingBatchStore.Received(3).AcquireJobAsync();
            await _testingBatchStore.Received(1).ReleaseJobAsync(job.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Finished));
        }

        [Fact(DisplayName = "Advance To Date Multiple Jobs")]
        public async Task Advance_To_Date_Multiple_Jobs()
        {
            //Arrange
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName,
            };


            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName,
                DueTime = new DateTime(2018, 2, 2)
            };

            _testingBatchStore.GetNextJobDueTimeAsync().Returns(Task.FromResult((DateTime?)job2.DueTime), Task.FromResult((DateTime?)null));

            var now = new DateTime(2000, 1, 1);
            var to = new DateTime(2019, 2, 2);

            _dateSimulationService.GetNow().Returns(now, now, now, job2.DueTime, job2.DueTime, to);

            _testingBatchStore.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null), Task.FromResult(job2), Task.FromResult((JobDescription)null));

            //Act
            await _engine.AdvanceToDateAsync(to);

            //Assert
            _dateSimulationService.Received(6).GetNow();
            _dateSimulationService.Received(1).SetNow(job2.DueTime);
            _dateSimulationService.Received(1).SetNow(to);
            await _testingBatchStore.Received(2).GetNextJobDueTimeAsync();
            await _testingBatchStore.Received(5).AcquireJobAsync();
            await _testingBatchStore.Received(1).ReleaseJobAsync(job.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Finished));
            await _testingBatchStore.Received(1).ReleaseJobAsync(job2.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Finished));
        }

        [Fact(DisplayName = "Advance To Date Job Should Throw Set To True Should Throw")]
        public async Task Advance_To_Date_Job_Should_Throw_Set_To_True_Should_Throw()
        {
            //Arrange
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithException).AssemblyQualifiedName,
            };

            var now = new DateTime(2000, 1, 1);
            var to = new DateTime(2019, 2, 2);

            _dateSimulationService.GetNow().Returns(now, now, now, to);

            _testingBatchStore.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            //Act
            var ex = await Assert.ThrowsAsync<NotImplementedException>(() => _engine.AdvanceToDateAsync(to));

            //Assert
            _dateSimulationService.Received(2).GetNow();
            _dateSimulationService.DidNotReceive().SetNow(Arg.Any<DateTime>());
            await _testingBatchStore.DidNotReceive().GetNextJobDueTimeAsync();
            await _testingBatchStore.Received(1).AcquireJobAsync();
            await _testingBatchStore.Received(1).ReleaseJobAsync(job.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Error));

            Assert.Equal("Method not implemented.", ex.Message);
        }

        [Fact(DisplayName = "Advance To Date Job Should Throw Set To False Should Not Throw")]
        public async Task Advance_To_Date_Job_Should_Throw_Set_To_False_Should_Not_Throw()
        {
            //Arrange
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithException).AssemblyQualifiedName,
            };

            var now = new DateTime(2000, 1, 1);
            var to = new DateTime(2019, 2, 2);

            _dateSimulationService.GetNow().Returns(now, now, now, to);

            _testingBatchStore.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            //Act
            await _engine.AdvanceToDateAsync(to, false);

            //Assert
            _dateSimulationService.Received(4).GetNow();
            _dateSimulationService.Received(1).SetNow(to);
            await _testingBatchStore.Received(1).GetNextJobDueTimeAsync();
            await _testingBatchStore.Received(3).AcquireJobAsync();
            await _testingBatchStore.Received(1).ReleaseJobAsync(job.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Error));

        }

        [Fact(DisplayName = "Skip To Date Single Job Should Set Date")]
        public async Task Skip_To_Date_Single_Job_Should_Set_Date()
        {
            //Arrange
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName,
            };

            var to = new DateTime(2019, 2, 2);

            _dateSimulationService.GetNow().Returns(to);

            _testingBatchStore.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            //Act
            await _engine.SkipToDate(to);

            //Assert
            _dateSimulationService.Received(4).GetNow();
            _dateSimulationService.Received(1).SetNow(to);
            await _testingBatchStore.DidNotReceive().GetNextJobDueTimeAsync();
            await _testingBatchStore.Received(2).AcquireJobAsync();
            await _testingBatchStore.Received(1).ReleaseJobAsync(job.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Finished));
        }

        [Fact(DisplayName = "Advance To Date Multiple Jobs Should Set Date")]
        public async Task Advance_To_Date_Multiple_Jobs_Should_Set_Date()
        {
            //Arrange
            var job = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName,
            };


            var job2 = new JobDescription
            {
                Id = Guid.NewGuid(),
                Type = typeof(TestJobWithoutDependencies).AssemblyQualifiedName,
                DueTime = new DateTime(2018, 2, 2)
            };

            _testingBatchStore.GetNextJobDueTimeAsync().Returns(Task.FromResult((DateTime?)job2.DueTime), Task.FromResult((DateTime?)null));

            var to = new DateTime(2019, 2, 2);

            _dateSimulationService.GetNow().Returns(to);

            _testingBatchStore.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult(job2), Task.FromResult((JobDescription)null));

            //Act
            await _engine.SkipToDate(to);

            //Assert
            _dateSimulationService.Received(5).GetNow();
            _dateSimulationService.Received(1).SetNow(to);
            await _testingBatchStore.DidNotReceive().GetNextJobDueTimeAsync();
            await _testingBatchStore.Received(3).AcquireJobAsync();
            await _testingBatchStore.Received(1).ReleaseJobAsync(job.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Finished));
            await _testingBatchStore.Received(1).ReleaseJobAsync(job2.Id, Arg.Is<JobResult>(x => x.State == ExecutionState.Finished));
        }
    }
}