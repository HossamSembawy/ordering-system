using FulfilmentService.Database;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FulfilmentService.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FulfilmentDbContext _dbContext;
        private Lazy<IGenericRepository<Worker>> _workerRepository;
        private Lazy<IGenericRepository<FulfillmentTask>> _taskRepository;
        private Lazy<IGenericRepository<Cursor>> _cursorRepository;


        public UnitOfWork(FulfilmentDbContext dbContext)
        {
            _workerRepository = new Lazy<IGenericRepository<Worker>>(() => new GenericRepository<Worker>(dbContext));
            _taskRepository = new Lazy<IGenericRepository<FulfillmentTask>>(() => new GenericRepository<FulfillmentTask>(dbContext));
            _cursorRepository = new Lazy<IGenericRepository<Cursor>>(() => new GenericRepository<Cursor>(dbContext));
            _dbContext = dbContext;
        }
        public IGenericRepository<Cursor> CursorRepository => _cursorRepository.Value;
        public IGenericRepository<Worker> WorkerRepository => _workerRepository.Value;
        public IGenericRepository<FulfillmentTask> taskRepository => _taskRepository.Value;

        public async Task<IDbContextTransaction> BeginTransaction(bool serializable)
        {
            if (serializable)
            {
                var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                return dbContextTransaction;
            }
            var transaction = await _dbContext.Database.BeginTransactionAsync();
            return transaction;
        }

        public async Task CommitTransaction()
        {
            await _dbContext.Database.CommitTransactionAsync();
        }


        public async Task RollbackTransaction()
        {
            await _dbContext.Database.RollbackTransactionAsync();
        }

        public async Task SaveChanges()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
