using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minion.Core.Models;

namespace Minion.Core.Interfaces
{
    public interface IBatchStore
	{
	    Task InitAsync();
		Task HeartBeatAsync(string machineName, int numberOfTasks, int pollingFrequency, int heartBeatFrequency);
		Task<JobDescription> AcquireJobAsync();
		Task ReleaseJobAsync(Guid jobId, JobResult result);
		Task AddJobsAsync(IEnumerable<JobDescription> jobs);
	}
}