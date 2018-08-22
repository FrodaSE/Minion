using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minion.Core;
using Minion.Core.Interfaces;
using Minion.Core.Models;
using Minion.InMemory;

namespace Minion.DotNetCoreSample
{
    class Program
    {
        static void Main(string[] args)
        {
            //Setup
            var dateService = new UtcDateService();
            var store = new InMemoryStorage(dateService);
            var resolver = new SimpleResolver(dateService);
            ILogger logger = null;
            var settings = new BatchSettings
            {
                HeartBeatFrequency = 2000,
                NumberOfParallelJobs = 2,
                PollingFrequency = 500
            };
            
            var scheduler = new JobScheduler(store, dateService);

            //Add a sequence of jobs
            var sequence = new Sequence();

            sequence.Add<SimpleJob>();
            sequence.Add<JobWithInput, JobWithInput.Input>(new JobWithInput.Input
            {
                Text = "this is awesome!"
            });

            //Queue the sequence
            scheduler.QueueAsync(sequence).Wait();

            //Add a single job
            scheduler.QueueAsync<RecurringJob>();

            //Start the engine
            using (var engine = new BatchEngine(store, resolver, logger, settings))
			{
				Console.WriteLine("Starting ...");

			    engine.Start();

				Console.ReadKey();
			}
		}
    }

    public class SimpleResolver : IDependencyResolver
    {
        private readonly IDateService _dateService;

        public SimpleResolver(IDateService dateService)
        {
            _dateService = dateService;
        }

        public bool Resolve(Type type, out object resolvedType)
        {
            if (type == typeof(IDateService))
            {
                resolvedType = _dateService;
                return true;
            }

            resolvedType = null;
            return false;
        }
    }

    public class SimpleJob : Job
    {
        public override async Task<JobResult> ExecuteAsync()
        {
            Console.WriteLine("Hello from simple job");

            return Finished();
        }
    }

    public class JobWithInput : Job<JobWithInput.Input>
    {
        public class Input
        {
            public string Text { get; set; }
        }

        public override async Task<JobResult> ExecuteAsync(Input input)
        {
            Console.WriteLine("Hello from job with input: " + input.Text);

            return Finished();
        }
    }

    public class RecurringJob : Job
    {
        private readonly IDateService _dateService;

        public RecurringJob(IDateService dateService)
        {
            _dateService = dateService;
        }

        public override async Task<JobResult> ExecuteAsync()
        {
            Console.WriteLine("Hello from recurring job, I will execute every 2 seconds");

            return Reschedule(_dateService.GetNow().AddSeconds(2));
        }
    }
}
