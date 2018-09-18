using System.Threading.Tasks;

namespace Minion.Core.Models
{
    public abstract class Job : JobBase
    {
        public abstract Task<JobResult> ExecuteAsync();
    }

    public abstract class Job<TInput> : JobInputBase
    {
        public abstract Task<JobResult> ExecuteAsync(TInput input);

        internal override Task<JobResult> DoExecuteAsync(object input)
        {
            return ExecuteAsync((TInput)input);
        }
    }
}