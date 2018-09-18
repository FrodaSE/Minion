using System;
using System.Threading.Tasks;

namespace Minion.Core.Models
{
    public abstract class JobBase
    {
        protected JobResult Finished() => new JobResult
        {
            State = ExecutionState.Finished
        };

        protected JobResult Reschedule(DateTime dueTime) => new JobResult
        {
            State = ExecutionState.Waiting,
            DueTime = dueTime
        };

        protected JobResult Halt() => new JobResult
        {
            State = ExecutionState.Halted
        };
    }

    public abstract class JobInputBase : JobBase
    {

        internal virtual Task<JobResult> DoExecuteAsync(object input)
        {
            throw new NotImplementedException();
        }

    }
}