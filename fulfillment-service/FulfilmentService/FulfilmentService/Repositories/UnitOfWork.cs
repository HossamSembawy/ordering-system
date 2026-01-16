using FulfilmentService.Database;
using FulfilmentService.Interfaces;

namespace FulfilmentService.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FulfilmentDbContext _dbContext;
        private Lazy<IFulfilmentTaskRepository> _fulfilmentTaskRepository;
        private Lazy<IWorkerRepository> _workerRepository;
        public UnitOfWork(FulfilmentDbContext dbContext)
        {
            _dbContext = dbContext;
            _fulfilmentTaskRepository = new Lazy<IFulfilmentTaskRepository>(() => new FulfilmentTaskRepository(_dbContext,_workerRepository.Value));
            _workerRepository = new Lazy<IWorkerRepository>(() => new WorkerRepository(_dbContext));
        }
        public IFulfilmentTaskRepository FulfilmentTaskRepository => _fulfilmentTaskRepository.Value;

        public IWorkerRepository WorkerRepository => _workerRepository.Value;

        public async Task<int> SaveChangesAsync()
        => await _dbContext.SaveChangesAsync();
    }
}
