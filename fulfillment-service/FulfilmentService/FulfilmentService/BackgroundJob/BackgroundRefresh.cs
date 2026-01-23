
using FulfilmentService.Interfaces;
using Microsoft.Extensions.Logging;

namespace FulfilmentService.BackgroundJob
{
    public class BackgroundRefresh : IHostedService, IDisposable
    {
        private Timer? _timer;
        private ILogger<BackgroundRefresh> _logger;

        private readonly IServiceScopeFactory _scopeFactory;


        private readonly SemaphoreSlim _lock = new(1, 1);

        public BackgroundRefresh(ILogger<BackgroundRefresh> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _lock?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(TimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            return Task.CompletedTask;
        }

        private void TimerTick(object? state)
        {
            // Fire-and-forget safely (exceptions handled inside)
            _ = CheckPendingTasks(state);
        }


        private async Task CheckPendingTasks(object? state)
        {
            _logger.LogInformation($"Started checking for pending tasks at {DateTime.Now.ToLongTimeString()}");

            if (!await _lock.WaitAsync(0))
                return;

            try
            {
                _logger.LogInformation("Started checking for pending tasks at {Time}", DateTimeOffset.Now);

                using var scope = _scopeFactory.CreateScope();

                // Resolve scoped services here
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                // Example DB check (make sure your service method is async)
                var pending = await taskService.GetPendingTasks();

                _logger.LogInformation("Found {Count} pending tasks.", pending?.Count);

                // Example action (ensure AssignTask is idempotent)
                foreach (var t in pending)
                {
                    await taskService.AssignTask(t.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking pending tasks.");
            }
            finally
            {
                _lock.Release();
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
