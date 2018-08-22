using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using NSubstitute;
using Xunit;

namespace Minion.Tests
{
	[Trait("Category", "Job Scheduler Tests")]
	public class JobSchedulerTests
	{
		private readonly IBatchStore _store;
		private readonly IDateService _dateService;
		private readonly IJobScheduler _scheduler;
		private readonly DateTime _now;

		public JobSchedulerTests()
		{
			_store = Substitute.For<IBatchStore>();
			_dateService = Substitute.For<IDateService>();
			_scheduler = new JobScheduler(_store, _dateService);

			_now = new DateTime(2018, 1, 2, 3, 4, 5);

			_dateService.GetNow().Returns(_now);
		}

		[Fact(DisplayName = "Queue Simple Sequence")]
		public async Task Queue_Simple_Sequence()
		{
			//Arrange
			var sequence = new Sequence();

			var jobId1 = sequence.Add<TestJobWithoutInput>();
			var jobId2 = sequence.Add<TestJobWithoutInput>();
			var jobId3 = sequence.Add<TestJobWithoutInput>();

			var batch = new Batch(sequence);

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			await _scheduler.QueueAsync(batch);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);
			var result3 = jobs.ElementAt(2);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);
			Assert.Equal(jobId3, result3.Id);

			Assert.Equal(batch.Id, result1.BatchId);
			Assert.Equal(batch.Id, result2.BatchId);
			Assert.Equal(batch.Id, result3.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);
			Assert.Equal(_now, result3.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
			Assert.Equal(_now, result3.UpdatedTime);

			Assert.Equal(0, result1.WaitCount);
			Assert.Equal(1, result2.WaitCount);
			Assert.Equal(1, result3.WaitCount);

			Assert.Null(result1.PrevId);
			Assert.Equal(result1.Id, result2.PrevId);
			Assert.Equal(result2.Id, result3.PrevId);

			Assert.Null(result1.NextId);
			Assert.Null(result2.NextId);
			Assert.Null(result3.NextId);
		}

		[Fact(DisplayName = "Queue Simple Set")]
		public async Task Queue_Simple_Set()
		{
			//Arrange
			var set = new Set();

			var jobId1 = set.Add<TestJobWithoutInput>();
			var jobId2 = set.Add<TestJobWithoutInput>();
			var jobId3 = set.Add<TestJobWithoutInput>();

			var batch = new Batch(set);

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			await _scheduler.QueueAsync(batch);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);
			var result3 = jobs.ElementAt(2);

			Assert.Equal(batch.Id, result1.BatchId);
			Assert.Equal(batch.Id, result2.BatchId);
			Assert.Equal(batch.Id, result3.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);
			Assert.Equal(_now, result3.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
			Assert.Equal(_now, result3.UpdatedTime);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);
			Assert.Equal(jobId3, result3.Id);

			Assert.Equal(0, result1.WaitCount);
			Assert.Equal(0, result2.WaitCount);
			Assert.Equal(0, result3.WaitCount);

			Assert.Null(result1.PrevId);
			Assert.Null(result2.PrevId);
			Assert.Null(result3.PrevId);

			Assert.Null(result1.NextId);
			Assert.Null(result2.NextId);
			Assert.Null(result3.NextId);
		}

		[Fact(DisplayName = "Queue Sequence With Set")]
		public async Task Queue_Sequence_With_Set()
		{
			//Arrange
			var sequence = new Sequence();

			var jobId1 = sequence.Add<TestJobWithoutInput>();

			var set = new Set();

			var jobId2 = set.Add<TestJobWithoutInput>();
			var jobId3 = set.Add<TestJobWithoutInput>();

			sequence.Add(set);

			var jobId4 = sequence.Add<TestJobWithoutInput>();

			var batch = new Batch(sequence);

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			await _scheduler.QueueAsync(batch);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);
			var result3 = jobs.ElementAt(2);
			var result4 = jobs.ElementAt(3);

			Assert.Equal(batch.Id, result1.BatchId);
			Assert.Equal(batch.Id, result2.BatchId);
			Assert.Equal(batch.Id, result3.BatchId);
			Assert.Equal(batch.Id, result4.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);
			Assert.Equal(_now, result3.CreatedTime);
			Assert.Equal(_now, result4.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
			Assert.Equal(_now, result3.UpdatedTime);
			Assert.Equal(_now, result4.UpdatedTime);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);
			Assert.Equal(jobId3, result3.Id);
			Assert.Equal(jobId4, result4.Id);

			Assert.Equal(0, result1.WaitCount);
			Assert.Equal(1, result2.WaitCount);
			Assert.Equal(1, result3.WaitCount);
			Assert.Equal(2, result4.WaitCount);

			Assert.Null(result1.PrevId);
			Assert.Equal(result1.Id, result2.PrevId);
			Assert.Equal(result1.Id, result3.PrevId);
			Assert.Null(result4.PrevId);

			Assert.Null(result1.NextId);
			Assert.Equal(result4.Id, result2.NextId);
			Assert.Equal(result4.Id, result3.NextId);
			Assert.Null(result4.NextId);
		}

		[Fact(DisplayName = "Queue Set With Sequence")]
		public async Task Queue_Set_With_Sequence()
		{
			//Arrange
			var set = new Set();

			var jobId1 = set.Add<TestJobWithoutInput>();

			var sequence = new Sequence();

			var jobId2 = sequence.Add<TestJobWithoutInput>();
			var jobId3 = sequence.Add<TestJobWithoutInput>();

			set.Add(sequence);

			var jobId4 = set.Add<TestJobWithoutInput>();

			var batch = new Batch(set);

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			await _scheduler.QueueAsync(batch);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);
			var result3 = jobs.ElementAt(2);
			var result4 = jobs.ElementAt(3);

			Assert.Equal(batch.Id, result1.BatchId);
			Assert.Equal(batch.Id, result2.BatchId);
			Assert.Equal(batch.Id, result3.BatchId);
			Assert.Equal(batch.Id, result4.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);
			Assert.Equal(_now, result3.CreatedTime);
			Assert.Equal(_now, result4.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
			Assert.Equal(_now, result3.UpdatedTime);
			Assert.Equal(_now, result4.UpdatedTime);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);
			Assert.Equal(jobId3, result3.Id);
			Assert.Equal(jobId4, result4.Id);

			Assert.Equal(0, result1.WaitCount);
			Assert.Equal(0, result2.WaitCount);
			Assert.Equal(1, result3.WaitCount);
			Assert.Equal(0, result4.WaitCount);

			Assert.Null(result1.PrevId);
			Assert.Null(result2.PrevId);
			Assert.Equal(result2.Id, result3.PrevId);
			Assert.Null(result4.PrevId);

			Assert.Null(result1.NextId);
			Assert.Null(result2.NextId);
			Assert.Null(result3.NextId);
			Assert.Null(result4.NextId);
		}

		[Fact(DisplayName = "Queue Set With Set Should Insert Sync Job")]
		public async Task Queue_Set_With_Set_Should_Insert_Sync_Job()
		{
			//Arrange

			var set1 = new Set();

			var jobId1 = set1.Add<TestJobWithoutInput>();
			var jobId2 = set1.Add<TestJobWithoutInput>();

			var set2 = new Set();

			var jobId3 = set2.Add<TestJobWithoutInput>();
			var jobId4 = set2.Add<TestJobWithoutInput>();

			var sequence = new Sequence();

			sequence.Add(set1);
			sequence.Add(set2);

			var batch = new Batch(sequence);

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			await _scheduler.QueueAsync(batch);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);
			var sync = jobs.ElementAt(2);
			var result3 = jobs.ElementAt(3);
			var result4 = jobs.ElementAt(4);

			Assert.Equal(batch.Id, result1.BatchId);
			Assert.Equal(batch.Id, result2.BatchId);
			Assert.Equal(batch.Id, sync.BatchId);
			Assert.Equal(batch.Id, result3.BatchId);
			Assert.Equal(batch.Id, result4.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);
			Assert.Equal(_now, sync.CreatedTime);
			Assert.Equal(_now, result3.CreatedTime);
			Assert.Equal(_now, result4.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
			Assert.Equal(_now, sync.UpdatedTime);
			Assert.Equal(_now, result3.UpdatedTime);
			Assert.Equal(_now, result4.UpdatedTime);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);
			Assert.NotEqual(Guid.Empty, sync.Id);
			Assert.Equal(jobId3, result3.Id);
			Assert.Equal(jobId4, result4.Id);

			Assert.Equal(0, result1.WaitCount);
			Assert.Equal(0, result2.WaitCount);
			Assert.Equal(2, sync.WaitCount);
			Assert.Equal(1, result3.WaitCount);
			Assert.Equal(1, result4.WaitCount);

			Assert.Null(result1.PrevId);
			Assert.Null(result2.PrevId);
			Assert.Null(sync.PrevId);
			Assert.Equal(sync.Id, result3.PrevId);
			Assert.Equal(sync.Id, result4.PrevId);

			Assert.Equal(sync.Id, result1.NextId);
			Assert.Equal(sync.Id, result2.NextId);

			Assert.Null(sync.NextId);
			Assert.Null(result3.NextId);
			Assert.Null(result4.NextId);

			Assert.Equal(typeof(SyncJob).AssemblyQualifiedName, sync.Type);
			Assert.Null(sync.Input);
			Assert.Equal(ExecutionState.Waiting, sync.State);
        }

		[Fact(DisplayName = "Queue Set")]
		public async Task Queue_Set()
		{
			//Arrange
			var set = new Set();

			var jobId1 = set.Add<TestJobWithoutInput>();
			var jobId2 = set.Add<TestJobWithoutInput>();

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			var batchId = await _scheduler.QueueAsync(set);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);

			Assert.Equal(batchId, result1.BatchId);
			Assert.Equal(batchId, result2.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
		}

		[Fact(DisplayName = "Queue Sequence")]
		public async Task Queue_Sequence()
		{
			//Arrange
			var sequence = new Sequence();

			var jobId1 = sequence.Add<TestJobWithoutInput>();
			var jobId2 = sequence.Add<TestJobWithoutInput>();

			IEnumerable<JobDescription> jobs = null;

			await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

			//Act
			var batchId = await _scheduler.QueueAsync(sequence);

			//Assert
			await _store.Received(1).AddJobsAsync(jobs);

			var result1 = jobs.ElementAt(0);
			var result2 = jobs.ElementAt(1);

			Assert.Equal(jobId1, result1.Id);
			Assert.Equal(jobId2, result2.Id);

			Assert.Equal(batchId, result1.BatchId);
			Assert.Equal(batchId, result2.BatchId);

			Assert.Equal(_now, result1.CreatedTime);
			Assert.Equal(_now, result2.CreatedTime);

			Assert.Equal(_now, result1.UpdatedTime);
			Assert.Equal(_now, result2.UpdatedTime);
	    }

	    [Fact(DisplayName = "Queue Job")]
	    public async Task Queue_Job()
	    {
	        //Arrange
	        IEnumerable<JobDescription> jobs = null;

	        await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

	        //Act
	        var batchId = await _scheduler.QueueAsync<TestJobWithoutInput>();

	        //Assert
	        await _store.Received(1).AddJobsAsync(jobs);

	        var result1 = jobs.ElementAt(0);

	        Assert.Equal(batchId, result1.BatchId);
	        Assert.Equal(_now, result1.CreatedTime);
	        Assert.Equal(_now, result1.UpdatedTime);
	    }

	    [Fact(DisplayName = "Queue Job With Data")]
	    public async Task Queue_Job_With_Data()
	    {
	        //Arrange
	        IEnumerable<JobDescription> jobs = null;

	        await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

	        //Act
	        var batchId = await _scheduler.QueueAsync<TestJob, int>(1);

	        //Assert
	        await _store.Received(1).AddJobsAsync(jobs);

	        var result1 = jobs.ElementAt(0);

	        Assert.Equal(batchId, result1.BatchId);
	        Assert.Equal(_now, result1.CreatedTime);
	        Assert.Equal(_now, result1.UpdatedTime);
	    }

	    [Fact(DisplayName = "Queue Job With DueTime")]
	    public async Task Queue_Job_With_DueTime()
	    {
	        //Arrange
	        IEnumerable<JobDescription> jobs = null;
	        var time = new DateTime(2016, 1, 2, 3, 4, 5);

	        await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

	        //Act
	        var batchId = await _scheduler.QueueAsync<TestJobWithoutInput>(time);

	        //Assert
	        await _store.Received(1).AddJobsAsync(jobs);

	        var result1 = jobs.ElementAt(0);

	        Assert.Equal(batchId, result1.BatchId);
	        Assert.Equal(_now, result1.CreatedTime);
	        Assert.Equal(_now, result1.UpdatedTime);
	        Assert.Equal(time, result1.DueTime);
        }

        [Fact(DisplayName = "Queue Job With Data And DueTime")]
        public async Task Queue_Job_With_Data_And_DueTime()
        {
            //Arrange
            IEnumerable<JobDescription> jobs = null;
            var time = new DateTime(2016, 1, 2, 3, 4, 5);

            await _store.AddJobsAsync(Arg.Do<IEnumerable<JobDescription>>(x => jobs = x));

            //Act
            var batchId = await _scheduler.QueueAsync<TestJob, int>(1, time);

            //Assert
            await _store.Received(1).AddJobsAsync(jobs);

            var result1 = jobs.ElementAt(0);

            Assert.Equal(batchId, result1.BatchId);
            Assert.Equal(_now, result1.CreatedTime);
            Assert.Equal(_now, result1.UpdatedTime);
            Assert.Equal(time, result1.DueTime);
        }
    }
}