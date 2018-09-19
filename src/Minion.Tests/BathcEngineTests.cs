using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Minion.Tests
{
    [Trait("Category", "Batch Engine Tests")]
    public class BathcEngineTests
    {
        private readonly IBatchStore _store;
        private readonly IDependencyResolver _dependencyResolver;
        private readonly ILogger _logger;

        public BathcEngineTests()
        {
            _store = Substitute.For<IBatchStore>();
            _dependencyResolver = Substitute.For<IDependencyResolver>();
            _logger = Substitute.For<ILogger>();
        }

        [Fact(DisplayName = "Start Without Store")]
        public void Start_Without_Store()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 1000,
                HeartBeatFrequency = 50,
            };

            InvalidOperationException ex;

            //Act
            using (var engine = new BatchEngine(null, null, null, settings))
            {
                ex = Assert.Throws<InvalidOperationException>(() => engine.Start());
            }

            //Assert
            Assert.Equal("Cannot start without storage.", ex.Message);
        }

        [Fact(DisplayName = "Batch Engine Should Send Heart Beat")]
        public async Task Batch_Engine_Should_Send_Heart_Beat()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 1000,
                HeartBeatFrequency = 50,
            };

            //Act
            using (var engine = new BatchEngine(_store, null, _logger, settings))
            {
                engine.Start();

                await Task.Delay(150);
            }

            //Assert
            await _store.Received().HeartBeatAsync(Environment.MachineName, 1, 1000, 50);

            AssertNoErrorsLogged();
        }

        [Fact(DisplayName = "Heart Neat Failed Should Log Error")]
        public async Task Heart_Neat_Failed_Should_Log_Error()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 500,
                HeartBeatFrequency = 50,
            };

            _store.HeartBeatAsync(Environment.MachineName, 1, 500, 50)
                .Throws(x => new Exception("Some error"));

            //Act
            using (var engine = new BatchEngine(_store, null, _logger, settings))
            {
                engine.Start();

                await Task.Delay(150);
            }

            //Assert
            await _store.Received().HeartBeatAsync(Environment.MachineName, 1, 500, 50);

            _logger.Received().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString().StartsWith("Error while sending heartbeat, System.Exception: Some error")),
                null,
                Arg.Any<Func<object, Exception, string>>());
        }

        [Fact(DisplayName = "Heart Beat Failed When Logger Is Null Should Swallow Error")]
        public async Task Heart_Beat_Failed_When_Logger_Is_Null_Should_Swallow_Error()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 500,
                HeartBeatFrequency = 50,
            };

            _store.HeartBeatAsync(Environment.MachineName, 1, 500, 50)
                .Throws(x => new Exception("Some error"));

            //Act
            using (var engine = new BatchEngine(_store, null, null, settings))
            {
                engine.Start();

                await Task.Delay(150);
            }

            //Assert
            await _store.Received().HeartBeatAsync(Environment.MachineName, 1, 500, 50);

            AssertNoErrorsLogged();
        }

        [Fact(DisplayName = "Semaphore Released After Done Executing")]
        public async Task Semaphore_Released_After_Done_Executing()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 50,
                HeartBeatFrequency = 5000,
            };

            var job1 = Task.FromResult(CreateJob(1));
            var job2 = Task.FromResult(CreateJob(2));

            _store.AcquireJobAsync().Returns(job1, job2, Task.FromResult((JobDescription)null));

            var service = Substitute.For<ITestService>();

            _dependencyResolver.Resolve(typeof(ITestService), out _).Returns(x =>
            {
                x[1] = service;

                return true;
            });

            //Act
            using (var engine = new BatchEngine(_store, _dependencyResolver, _logger, settings))
            {
                engine.Start();

                await Task.Delay(150);
            }

            //Assert
            Received.InOrder(() =>
            {
                service.DoSomethingAsync(1);
                service.DoSomethingAsync(2);
            });

            await _store.Received().AcquireJobAsync();
            await service.Received(2).DoSomethingAsync(Arg.Any<int>());

            AssertNoErrorsLogged();
        }

        [Fact(DisplayName = "Should Wait For Executing Job To Finish Before Shut Down")]
        public async Task Should_Wait_For_Executing_Job_To_Finish_Before_Shut_Down()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 50,
                HeartBeatFrequency = 5000,
            };

            var job = Task.FromResult(CreateDelayedJob(300));

            _store.AcquireJobAsync().Returns(job, Task.FromResult((JobDescription)null));

            var service = Substitute.For<ITestService>();

            _dependencyResolver.Resolve(typeof(ITestService), out _).Returns(x =>
            {
                x[1] = service;

                return true;
            });

            //Act
            using (var engine = new BatchEngine(_store, _dependencyResolver, _logger, settings))
            {
                engine.Start();

                await Task.Delay(10);
            }

            //Assert
            await service.Received(1).DoSomethingAsync(300);

            AssertNoErrorsLogged();
        }

        [Fact(DisplayName = "Successful Job Should Release With Given Result")]
        public async Task Successful_Job_Should_Release_With_Given_Result()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 50,
                HeartBeatFrequency = 5000,
            };

            var data = new TestJobWithReturnData.TestData
            {
                State = ExecutionState.Halted,
                StatusInfo = "info",
                DueTime = new DateTime(2018, 1, 2, 3, 4, 5)
            };

            var job = CreateJobWithReturnData(data);

            job.Id = Guid.NewGuid();

            _store.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            JobResult result = null;

            await _store.ReleaseJobAsync(job.Id, Arg.Do<JobResult>(x => result = x));

            //Act
            using (var engine = new BatchEngine(_store, null, _logger, settings))
            {
                engine.Start();

                await Task.Delay(10);
            }

            //Assert
            Assert.Equal(data.State, result.State);
            Assert.Equal(data.StatusInfo, result.StatusInfo);
            Assert.Equal(data.DueTime, result.DueTime);

            await _store.Received(1).ReleaseJobAsync(job.Id, result);

            AssertNoErrorsLogged();
        }

        [Fact(DisplayName = "Job Throws Exception Should Release With Error State")]
        public async Task Job_Throws_Exception_Should_Release_With_Error_State()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 50,
                HeartBeatFrequency = 5000,
            };

            var job = CreateJobWithException();

            _store.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            JobResult result = null;

            await _store.ReleaseJobAsync(job.Id, Arg.Do<JobResult>(x => result = x));

            //Act
            using (var engine = new BatchEngine(_store, null, _logger, settings))
            {
                engine.Start();

                await Task.Delay(10);
            }

            //Assert
            Assert.Equal(ExecutionState.Error, result.State);
            Assert.StartsWith("System.NotImplementedException: Method not implemented.", result.StatusInfo);
            Assert.Equal(job.DueTime, result.DueTime);

            await _store.Received(1).ReleaseJobAsync(job.Id, result);

            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(x => x.ToString().StartsWith("Error while processing work item, ")),
                null,
                Arg.Any<Func<object, Exception, string>>());
        }

        [Fact(DisplayName = "WorkItem Throws Exception When Logger Is Null Should Swallow Exception")]
        public async Task WorkItem_Throws_Exception_When_Logger_Is_Null_Should_Swallow_Exception()
        {
            //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 50,
                HeartBeatFrequency = 5000,
            };

            var job = CreateJobWithException();

            _store.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null));

            //Act
            using (var engine = new BatchEngine(_store, null, null, settings))
            {
                engine.Start();

                await Task.Delay(10);
            }

            //Assert

            AssertNoErrorsLogged();
        }

        [Fact(DisplayName = "Polling Frequency Zero Should Release Semaphore")]
        public async Task Polling_Frequency_Zero_Should_Release_Semaphore()
        {    //Arrange
            var settings = new BatchSettings
            {
                NumberOfParallelJobs = 1,
                PollingFrequency = 0,
                HeartBeatFrequency = 5000,
            };

            var data = new TestJobWithReturnData.TestData
            {
                State = ExecutionState.Waiting,
                StatusInfo = "info",
                DueTime = new DateTime(2018, 1, 2, 3, 4, 5)
            };

            var job = CreateJobWithReturnData(data);

            job.Id = Guid.NewGuid();

            _store.AcquireJobAsync().Returns(Task.FromResult(job), Task.FromResult((JobDescription)null), Task.FromResult(job), Task.FromResult((JobDescription)null));

            //Act
            using (var engine = new BatchEngine(_store, null, _logger, settings))
            {
                engine.Start();

                await Task.Delay(200);
            }

            //Assert

            await _store.Received(2).ReleaseJobAsync(job.Id, Arg.Any<JobResult>());

            AssertNoErrorsLogged();
        }

        private JobDescription CreateJob(int data)
        {
            return new JobDescription
            {
                Type = typeof(TestJob).AssemblyQualifiedName,
                Input = new JobInputDescription
                {
                    Type = data.GetType().AssemblyQualifiedName,
                    InputData = data
                }
            };
        }

        private JobDescription CreateDelayedJob(int data)
        {
            return new JobDescription
            {
                Type = typeof(TestJobWithDelay).AssemblyQualifiedName,
                Input = new JobInputDescription
                {
                    Type = data.GetType().AssemblyQualifiedName,
                    InputData = data
                }
            };
        }

        private JobDescription CreateJobWithReturnData(TestJobWithReturnData.TestData data)
        {
            return new JobDescription
            {
                Type = typeof(TestJobWithReturnData).AssemblyQualifiedName,
                Input = new JobInputDescription
                {
                    Type = data.GetType().AssemblyQualifiedName,
                    InputData = data
                }
            };
        }

        private JobDescription CreateJobWithException()
        {
            return new JobDescription
            {
                Type = typeof(TestJobWithException).AssemblyQualifiedName,
            };
        }

        private void AssertNoErrorsLogged()
        {
            _logger.DidNotReceive().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                null,
                Arg.Any<Func<object, Exception, string>>());
        }

    }
}