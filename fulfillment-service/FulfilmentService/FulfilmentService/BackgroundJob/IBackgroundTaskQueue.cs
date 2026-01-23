namespace FulfilmentService.BackgroundJob
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);
        Task<Func<IServiceProvider, CancellationToken,Task>> DequeueBackgroundWorkItemAsync(CancellationToken cancellationToken);
    }
}
