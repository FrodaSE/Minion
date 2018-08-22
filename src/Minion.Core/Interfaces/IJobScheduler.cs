using System;
using System.Threading.Tasks;
using Minion.Core.Models;

namespace Minion.Core.Interfaces
{
	public interface IJobScheduler
	{
		/// <summary>
		/// Queue a batch of jobs
		/// </summary>
		/// <param name="batch">The batch to queue</param>
		/// <returns>Task</returns>
		Task QueueAsync(Batch batch);

		/// <summary>
		/// Queue a set of jobs
		/// </summary>
		/// <param name="set">The set to queue</param>
		/// <returns>The od of the batch</returns>
		Task<Guid> QueueAsync(Set set);

		/// <summary>
		/// Queue a sequence of jobs
		/// </summary>
		/// <param name="sequence">The sequence to queue</param>
		/// <returns>The id of the batch</returns>
		Task<Guid> QueueAsync(Sequence sequence);

		/// <summary>
		/// Queue a job
		/// </summary>
		/// <returns>The id of the batch</returns>
		Task<Guid> QueueAsync<TJob>()
			where TJob : Job;

		/// <summary>
		/// Queue a job
		/// </summary>
		/// <param name="input">The input data for the job</param>
		/// <returns>The id of the batch</returns>
		Task<Guid> QueueAsync<TJob, TInput>(TInput input)
			where TJob : Job<TInput>;
	}
}