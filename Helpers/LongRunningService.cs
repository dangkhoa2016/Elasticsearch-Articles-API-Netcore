using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Helpers
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio#queued-background-tasks
    /// </summary>
    public class LongRunningService : BackgroundService
    {
        private readonly ILogger<LongRunningService> _logger;
        public IBackgroundWorkerQueue TaskQueue { get; }

        public LongRunningService(IBackgroundWorkerQueue taskQueue, ILogger<LongRunningService> logger)
        {
            TaskQueue = taskQueue;
            _logger = logger;
        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Long Running Service] is starting.");

            await base.StartAsync(cancellationToken);
        }


        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Long Running Service] Error occurred executing {nameof(workItem)}.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("[Long Running Service] is stopping.");

            await base.StopAsync(cancellationToken);
        }
    }
}
