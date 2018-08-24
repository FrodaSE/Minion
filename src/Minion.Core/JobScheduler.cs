using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minion.Core.Interfaces;
using Minion.Core.Models;

namespace Minion.Core
{
	public class JobScheduler : IJobScheduler
	{
		private readonly IBatchStore _store;
		private readonly IDateService _dateService;

	    public JobScheduler()
	    {
	        _store = MinionConfiguration.Configuration.Store;
	        _dateService = MinionConfiguration.Configuration.DateService;
	    }

        [Obsolete("Only used for testing")]
		internal JobScheduler(IBatchStore store, IDateService dateService)
		{
			_store = store;
			_dateService = dateService;
		}

		/// <summary>
		/// Queue a batch of jobs
		/// </summary>
		/// <param name="batch">The batch to queue</param>
		/// <returns>Task</returns>
		public async Task QueueAsync(Batch batch)
		{
			var jobs = new List<JobDescription>();

			Process(batch.RootItem, jobs);

			var now = _dateService.GetNow();

			foreach (var job in jobs)
			{
				job.BatchId = batch.Id;
				job.CreatedTime = now;
				job.UpdatedTime = now;
			}

			await _store.AddJobsAsync(jobs);
		}

		/// <summary>
		/// Queue a set of jobs
		/// </summary>
		/// <param name="set">The set to queue</param>
		/// <returns>The od of the batch</returns>
		public async Task<Guid> QueueAsync(Set set)
		{
			var batch = new Batch(set);

			await QueueAsync(batch);

			return batch.Id;
		}

		/// <summary>
		/// Queue a sequence of jobs
		/// </summary>
		/// <param name="sequence">The sequence to queue</param>
		/// <returns>The id of the batch</returns>
		public async Task<Guid> QueueAsync(Sequence sequence)
		{
			var batch = new Batch(sequence);

			await QueueAsync(batch);

			return batch.Id;
		}

		/// <summary>
		/// Queue a job
		/// </summary>
		/// <returns>The id of the batch</returns>
		public Task<Guid> QueueAsync<TJob>()
			where TJob : Job
		{
			var sequence = new Sequence();

			sequence.Add<TJob>();

			return QueueAsync(sequence);
		}

		/// <summary>
		/// Queue a job
		/// </summary>
		/// <param name="input">The input data for the job</param>
		/// <returns>The id of the batch</returns>
		public Task<Guid> QueueAsync<TJob, TInput>(TInput input)
			where TJob : Job<TInput>
		{
			var sequence = new Sequence();

			sequence.Add<TJob, TInput>(input);

			return QueueAsync(sequence);
		}

	    /// <summary>
	    /// Queue a job
	    /// </summary>
	    /// <param name="dueTime">The time when to start executing</param>
	    /// <returns>The id of the batch</returns>
        public Task<Guid> QueueAsync<TJob>(DateTime dueTime) where TJob : Job
	    {
	        var sequence = new Sequence();

	        sequence.Add<TJob>(dueTime);

	        return QueueAsync(sequence);
        }

	    /// <summary>
	    /// Queue a job
	    /// </summary>
	    /// <param name="input">The input data for the job</param>
	    /// <param name="dueTime">The time when to start executing</param>
	    /// <returns>The id of the batch</returns>
        public Task<Guid> QueueAsync<TJob, TInput>(TInput input, DateTime dueTime) where TJob : Job<TInput>
	    {
	        var sequence = new Sequence();

	        sequence.Add<TJob, TInput>(input, dueTime);

	        return QueueAsync(sequence);
        }

	    private JobDescription[] Process(JobScope jobScope, List<JobDescription> jobSet,
			params JobDescription[] previous)
		{
			switch (jobScope)
			{
				case Sequence sequence:
					return Process(sequence, jobSet, previous);
				case Set set:
					return Process(set, jobSet, previous);
				default:
					throw new InvalidOperationException();
			}
		}

		private JobDescription[] Process(Sequence sequence, List<JobDescription> jobSet, params JobDescription[] previous)
		{
			foreach (var item in sequence.Items)
			{
				if (item is JobScope jobScope)
				{
					previous = Process(jobScope, jobSet, previous);
					continue;
				}

				var job = (JobDescription)item;

				if (previous.Length > 1) //previous was a set 
				{
					foreach (var prev in previous)
					{
						prev.NextId = job.Id;
						job.WaitCount++;
					}
				}
				else if (previous.Length == 1) //Previous was a sequence 
				{
					job.PrevId = previous[0].Id;
					job.WaitCount = 1;
				}

				previous = new[] { job };
				jobSet.Add(job);
			}

			return previous;
		}

		private JobDescription[] Process(Set set, List<JobDescription> jobSet, params JobDescription[] previous)
		{
			if (previous.Length > 1) //Previous was a set
			{
				//We need to insert a sync job

				var syncJob = new JobDescription
				{
					Type = typeof(SyncJob).AssemblyQualifiedName,
					Input = null,
					Id = Guid.NewGuid(),
                    State = ExecutionState.Waiting
				};

				foreach (var prev in previous)
				{
					prev.NextId = syncJob.Id;
					syncJob.WaitCount++;
				}

				jobSet.Add(syncJob);
				previous = new[] { syncJob };
			}

			var result = new List<JobDescription>();

			foreach (var item in set.Items)
			{
				if (item is JobScope jobScope)
				{
					result.AddRange(Process(jobScope, jobSet, previous));
					continue;
				}

				var job = (JobDescription)item;

				if (previous.Length > 0)
				{
					job.PrevId = previous[0].Id;
					job.WaitCount = 1;
				}
				else
				{
					job.WaitCount = 0;
				}

				jobSet.Add(job);
				result.Add(job);
			}

			return result.ToArray();
		}
	}
}