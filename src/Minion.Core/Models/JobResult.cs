using System;

namespace Minion.Core.Models
{
	public class JobResult
	{
		public ExecutionState State { get; set; }
		public string StatusInfo { get; set; }
		public DateTime DueTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
	}
}