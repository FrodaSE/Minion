using System.Threading.Tasks;
using Minion.Core.Models;

namespace Minion.Core.Interfaces
{
	public interface IJobExecutor
	{
		Task<JobResult> ExecuteAsync(JobDescription job);
	}
}