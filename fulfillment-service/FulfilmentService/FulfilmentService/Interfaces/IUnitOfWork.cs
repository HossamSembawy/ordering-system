namespace FulfilmentService.Interfaces
{
    public interface IUnitOfWork
    {
        IFulfilmentTaskRepository FulfilmentTaskRepository { get; }
        IWorkerRepository WorkerRepository { get; }
        Task<int> SaveChangesAsync();

    }
}
