using FulfilmentService.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace FulfilmentService.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<Cursor> CursorRepository { get; }
        IGenericRepository<Worker> WorkerRepository { get; }
        IGenericRepository<FulfillmentTask> taskRepository { get; }
        Task SaveChanges();
        Task<IDbContextTransaction> BeginTransaction(bool serializable);
        Task CommitTransaction();
        Task RollbackTransaction();
    }
}
