using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using Newtonsoft.Json;

namespace Minion.InMemory
{
    public class InMemoryStorage : ITestingBatchStore, IBatchStore
    {
        private readonly IDateService _dateService;
        private readonly object _lock = new object();
        private readonly HashSet<JobDescription> _jobs = new HashSet<JobDescription>();

        public InMemoryStorage()
        {
            _dateService = MinionConfiguration.Configuration.DateService;
        }

        [Obsolete("Only used for testing")]
        internal InMemoryStorage(IDateService dateService)
        {
            _dateService = dateService;
        }

        public Task InitAsync()
        {
            return Task.FromResult(false);
        }

        public Task HeartBeatAsync(string machineName, int numberOfTasks, int pollingFrequency, int heartBeatFrequency)
        {
            return Task.FromResult(false);
        }

        public Task<JobDescription> AcquireJobAsync()
        {
            var now = _dateService.GetNow();

            lock (_lock)
            {
                var job = _jobs
                    .Where(x => x.State == ExecutionState.Waiting)
                    .Where(x => x.WaitCount == 0)
                    .Where(x => x.DueTime <= now)
                    .OrderBy(x => x.DueTime)
                    .ThenByDescending(x => x.Priority)
                    .FirstOrDefault();

                if (job == null)
                    return Task.FromResult((JobDescription)null);

                job.UpdatedTime = now;
                job.State = ExecutionState.Running;

                var copy = Copy(job);

                if (copy.Input != null)
                    copy.Input.InputData = JsonConvert.DeserializeObject((string)copy.Input.InputData, Type.GetType(copy.Input.Type));

                return Task.FromResult(copy);
            }
        }

        public Task ReleaseJobAsync(Guid jobId, JobResult result)
        {
            lock (_lock)
            {
                var job = _jobs.Single(x => x.Id == jobId);

                job.State = result.State;
                job.StatusInfo = result.StatusInfo;
                job.DueTime = result.DueTime;

                if (job.State == ExecutionState.Finished)
                {
                    var jobs = _jobs.Where(x => x.PrevId == job.Id || x.Id == job.NextId);

                    foreach (var nextJob in jobs)
                    {
                        nextJob.WaitCount--;
                    }
                }
            }

            return Task.FromResult(false);
        }

        public Task AddJobsAsync(IEnumerable<JobDescription> jobs)
        {
            var copies = new List<JobDescription>();

            foreach (var job in jobs)
            {
                var copy = Copy(job);

                if (copy.Input != null)
                {
                    copy.Input.InputData = JsonConvert.SerializeObject(copy.Input.InputData);
                }

                copies.Add(copy);
            }

            lock (_lock)
            {
                foreach (var job in copies)
                {
                    _jobs.Add(job);
                }
            }

            return Task.FromResult(false);

        }

        public Task<DateTime?> GetNextJobDueTimeAsync()
        {
            lock (_lock)
            {
                var job = _jobs
                    .Where(x => x.State == ExecutionState.Waiting)
                    .Where(x => x.WaitCount == 0)
                    .OrderBy(x => x.DueTime)
                    .FirstOrDefault();

                return Task.FromResult(job?.DueTime);
            }
        }

        private JobDescription Copy(JobDescription job)
        {
            var jobDescription = new JobDescription
            {
                Id = job.Id,
                Type = job.Type,
                DueTime = job.DueTime,
                Priority = job.Priority,
                WaitCount = job.WaitCount,
                PrevId = job.PrevId,
                NextId = job.NextId,
                BatchId = job.BatchId,
                CreatedTime = job.CreatedTime,
                UpdatedTime = job.UpdatedTime,
                Input = job.Input != null
                    ? new JobInputDescription
                    {
                        Type = job.Input.Type,
                        InputData = job.Input.InputData
                    }
                    : null,
                State = job.State,
                StatusInfo = job.StatusInfo
            };

            return jobDescription;
        }
    }
}
