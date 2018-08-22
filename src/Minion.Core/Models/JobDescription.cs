using System;

namespace Minion.Core.Models
{
	public class JobDescription : IUnit
	{
		public Guid Id { get; set; }
		public string Type { get; set; }
		public DateTime DueTime { get; set; }
		public int Priority { get; set; }
		public int WaitCount { get; set; }
		public Guid? PrevId { get; set; }
		public Guid? NextId { get; set; }
		public Guid BatchId { get; set; }
		public DateTime CreatedTime { get; set; }
		public DateTime UpdatedTime { get; set; }
		public JobInputDescription Input { get; set; }
        public ExecutionState State { get; set; }
	    public string StatusInfo { get; set; }
	}
}