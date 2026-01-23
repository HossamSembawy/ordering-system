namespace FulfilmentService.BackgroundJob
{
    public class QueuedProcessorBackgroundService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedProcessorBackgroundService> _logger;

        public QueuedProcessorBackgroundService(IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory, 
            ILogger<QueuedProcessorBackgroundService> logger)
        {
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken = default!)
        {
            _logger.LogInformation("Queued Processor Background Service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueBackgroundWorkItemAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var scopedProvider = scope.ServiceProvider;

                try
                {
                    await workItem(scopedProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred executing {nameof(workItem)}.");
                }
            }

            _logger.LogInformation("Queued Processor Background Service is stopping.");
        }
    }
}
