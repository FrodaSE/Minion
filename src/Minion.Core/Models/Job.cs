using System.Threading.Tasks;

namespace Minion.Core.Models
{
	public abstract class Job : JobBase
	{
		public abstract Task<JobResult> ExecuteAsync();
	}

	public abstract class Job<TInput> : JobBase
	{
		public abstract Task<JobResult> ExecuteAsync(TInput input);
	}
}