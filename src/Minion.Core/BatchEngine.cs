using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minion.Core.Interfaces;
using Minion.Core.Models;

namespace Minion.Core
{
    public class BatchEngine : IDisposable
    {
        private readonly IBatchStore _store;
        private readonly IJobExecutor _jobExecutor;
        private readonly ILogger _logger;
        private readonly BatchSettings _settings;

        private CancellationTokenSource _cts;
        private Task _engineTask;

        private Task _heartBeatTask;

        public BatchEngine()
        {
            _store = MinionConfiguration.Configuration.Store;
            _jobExecutor = new DependencyInjectionJobExecutor(MinionConfiguration.Configuration.DependencyResolver);
            _logger = MinionConfiguration.Configuration.Logger;
            _settings = new BatchSettings
            {
                HeartBeatFrequency = MinionConfiguration.Configuration.HeartBeatFrequency,
                NumberOfParallelJobs = MinionConfiguration.Configuration.NumberOfParallelJobs,
                PollingFrequency = MinionConfiguration.Configuration.PollingFrequency,
            };
        }

        [Obsolete("Only used for testing")]
        internal BatchEngine(IBatchStore store, IDependencyResolver resolver, ILogger logger, BatchSettings batchSettings)
        {
            _store = store;
            _jobExecutor = new DependencyInjectionJobExecutor(resolver);
            _logger = logger;
            _settings = batchSettings ?? new BatchSettings();
        }

        public void Dispose()
        {
            _cts?.Cancel();

            if (_heartBeatTask != null)
                Task.WaitAll(_heartBeatTask);

            if (_engineTask != null)
                Task.WaitAll(_engineTask);

            _cts?.Dispose();
            _cts = null;
        }

        public void Start()
        {
            if (_store == null)
                throw new InvalidOperationException("Cannot start without storage.");

            _cts = new CancellationTokenSource();

            Task.Run(() => _store.InitAsync(), _cts.Token).Wait();

            _heartBeatTask = HeartBeatAsync(_cts.Token);
            _engineTask = ExecuteAsync(_cts.Token);
        }

        private async Task HeartBeatAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _store.HeartBeatAsync(Environment.MachineName, _settings.NumberOfParallelJobs,
                        _settings.PollingFrequency, _settings.HeartBeatFrequency);
                }
                catch (Exception e)
                {
                    _logger?.LogError("Error while sending heartbeat, {error}", e);
                }

                try
                {
                    await Task.Delay(_settings.HeartBeatFrequency, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                using (var semaphore = new SemaphoreSlim(_settings.NumberOfParallelJobs))
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await semaphore.WaitAsync(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        var job = await _store.AcquireJobAsync();

                        if (job == null)
                        {
                            try
                            {
                                if (_settings.PollingFrequency <= 0)
                                    continue;

                                await Task.Delay(_settings.PollingFrequency, token);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }
                        else
                        {
#pragma warning disable 4014
                            ExecuteAndReleaseJobAsync(job)
                                .ContinueWith(_ => semaphore.Release());
#pragma warning restore 4014
                        }
                    }

                    for (var i = 0; i < _settings.NumberOfParallelJobs; i++)
                        await semaphore.WaitAsync();
                }
            }
            catch (Exception e)
            {
                //This is most likely caused if store.AcquireJobAsync() or _store.ReleaseJobAsync(workitem) throws an exception
                //TODO: PEBR: How should this be handled? Just crash? Or should we wait x seconds and just restart the Task? Maybe try to restart it n times before crashing?
                _logger?.LogError("Error while processing, {error}", e);
                throw;
            }
        }

        private async Task ExecuteAndReleaseJobAsync(JobDescription job)
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

                _logger?.LogError("Error while processing work item, {error}", e);
            }
            finally
            {
                await _store.ReleaseJobAsync(job.Id, result);
            }
        }
    }
}