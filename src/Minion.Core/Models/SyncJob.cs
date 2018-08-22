using System.Threading.Tasks;

namespace Minion.Core.Models
{
	public class SyncJob : Job
	{
		public override Task<JobResult> ExecuteAsync()
		{
			return Task.FromResult(Finished());
		}
	}
}