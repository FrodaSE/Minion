using System;
using Minion.Core.Models;

namespace Minion.Sql
{
    public class JobDescriptionSqlModel
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
        public string InputType { get; set; }
        public string InputData { get; set; }
        public ExecutionState State { get; set; }
        public string StatusInfo { get; set; }
        public long ExecutionTime { get; set; }
    }
}