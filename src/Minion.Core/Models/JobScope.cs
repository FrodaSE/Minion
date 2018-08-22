using System;
using System.Collections.Generic;

namespace Minion.Core.Models
{
	public abstract class JobScope : IUnit
	{

		public readonly List<IUnit> Items = new List<IUnit>();

		public void Add(JobScope jobScope)
		{
			Items.Add(jobScope);
		}

		public Guid Add<TJob>()
			where TJob : Job
		{
			return Add<TJob>(DateTime.MinValue, 0);
		}

		public Guid Add<TJob>(int priority)
			where TJob : Job
		{
			return Add<TJob>(DateTime.MinValue, priority);
		}

		public Guid Add<TJob>(DateTime dueTime)
			where TJob : Job
		{
			return Add<TJob>(dueTime, 0);
		}

		public Guid Add<TJob>(DateTime dueTime, int priority)
			where TJob : Job
		{
			var description = new JobDescription
			{
				Id = Guid.NewGuid(),
				DueTime = dueTime,
				Priority = priority,
				Type = typeof(TJob).AssemblyQualifiedName,
				Input = null,
                State = ExecutionState.Waiting
			};

			Items.Add(description);

			return description.Id;
		}

		public Guid Add<TJob, TInput>(TInput input)
			where TJob : Job<TInput>
		{
			return Add<TJob, TInput>(input, DateTime.MinValue, 0);
		}

		public Guid Add<TJob, TInput>(TInput input, int priority)
			where TJob : Job<TInput>
		{
			return Add<TJob, TInput>(input, DateTime.MinValue, priority);
		}

		public Guid Add<TJob, TInput>(TInput input, DateTime dueTime)
			where TJob : Job<TInput>
		{
			return Add<TJob, TInput>(input, dueTime, 0);
		}

		public Guid Add<TJob, TInput>(TInput input, DateTime dueTime, int priority)
			where TJob : Job<TInput>
		{
			var description = new JobDescription
			{
				Id = Guid.NewGuid(),
				DueTime = dueTime,
				Priority = priority,
				Type = typeof(TJob).AssemblyQualifiedName,
				Input = new JobInputDescription
				{
					Type = typeof(TInput).AssemblyQualifiedName,
					InputData = input
				},
			    State = ExecutionState.Waiting
            };

			Items.Add(description);

			return description.Id;
		}


	}
}