using System;

namespace Minion.Core.Models
{
	public class Batch
	{
		public Guid Id { get; set; }
		public JobScope RootItem { get; }

		public Batch(JobScope jobScope)
		{
			Id = Guid.NewGuid();
			RootItem = jobScope;
		}
	}
}