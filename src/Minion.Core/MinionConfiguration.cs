using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minion.Core.Interfaces;

namespace Minion.Core
{
    public sealed class MinionConfiguration
    {
        private readonly DatePassThruService _dateService = new DatePassThruService(new UtcDateService());

        public static MinionConfiguration Configuration { get; } = new MinionConfiguration();

        public IBatchStore Store { get; private set; }
        public IDateService DateService => _dateService;
        public IDependencyResolver DependencyResolver { get; private set; }
        public ILogger Logger { get; private set; }

        public int HeartBeatFrequency { get; set; }
        public int NumberOfParallelJobs { get; set; }
        public int PollingFrequency { get; set; }

        static MinionConfiguration() { }
        private MinionConfiguration() { }

        public void UseDateService(IDateService dateService)
        {
            _dateService.UseDateService(dateService);
        }

        public void UseBatchStore(IBatchStore store)
        {
            Store = store;
            
            var cts = new CancellationTokenSource();
            Task.Run(() => Store.InitAsync(), cts.Token).Wait(cts.Token);
        }

        public void UseDependencyResolver(IDependencyResolver resolver)
        {
            DependencyResolver = resolver;
        }

        public void UseLogger(ILogger logger)
        {
            Logger = logger;
        }
    }
}