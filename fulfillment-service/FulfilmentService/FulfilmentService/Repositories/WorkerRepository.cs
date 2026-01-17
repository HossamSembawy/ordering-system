using FulfilmentService.Database;
using FulfilmentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FulfilmentService.Repositories
{
    public class WorkerRepository : IWorkerRepository
    {
        private readonly FulfilmentDbContext _dbContext;

        public WorkerRepository(FulfilmentDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public  async Task<int> GetCountActiveTasks(int workerId)
        {
            var worker = await _dbContext.Workers.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workerId);
            return worker!.ActiveTasksCount;
        }
    }
}
