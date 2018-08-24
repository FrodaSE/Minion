using System;
using System.Threading.Tasks;
using Minion.Core.Interfaces;
using Minion.Core.Models;

namespace Minion.Core
{
    public class TestingBatchEngine
    {
        private readonly ITestingBatchStore _store;
        private readonly IDateSimulationService _dateService;
        private readonly IJobExecutor _jobExecutor;

        // TODO: PEBR: Rethink how this is configured
        public TestingBatchEngine(ITestingBatchStore store, IDateSimulationService dateService, IDependencyResolver resolver = null)
        {
            _store = store;
            _dateService = dateService;
            _jobExecutor = new DependencyInjectionJobExecutor(resolver);
        }

        public Task AdvanceToDateAsync(DateTime date, bool throws = true)
        {
            return TickTo(date, throws);
        }

        public Task SkipToDate(DateTime date, bool throws = true)
        {
            if (date < _dateService.GetNow())
                throw new ArgumentException("Cannot go backwards through time.");

            _dateService.SetNow(date);

            return TickTo(date, throws);
        }

        private async Task TickTo(DateTime toDate, bool throws)
        {
            if (toDate < _dateService.GetNow())
                throw new ArgumentException("Cannot run to a date prior to what is currently now in the date simulation service.");

            while (true)
            {
                var now = _dateService.GetNow();

                var job = await _store.AcquireJobAsync();

                if (job != null)
                {

                    await ExecuteAndReleaseJobAsync(job, throws);

                }
                else if (now < toDate)
                {

                    var next = await _store.GetNextJobDueTimeAsync();

                    _dateService.SetNow(next ?? toDate);

                }
                else
                {
                    break;
                }
            }
        }

        private async Task ExecuteAndReleaseJobAsync(JobDescription job, bool throws)
        {
            JobResult result = null;

            try
            {
                result = await _jobExecutor.ExecuteAsync(job);
            }
            catch (Exception e)
            {
                result = new JobResult
                {
                    DueTime = job.DueTime,
                    State = ExecutionState.Error,
                    StatusInfo = e.ToString()
                };

                if (throws)
                {
                    throw;
                }

            }
            finally
            {
                await _store.ReleaseJobAsync(job.Id, result);
            }
        }
    }
}