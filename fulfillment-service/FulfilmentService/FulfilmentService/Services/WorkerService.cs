using FulfilmentService.Database;
using FulfilmentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FulfilmentService.Services
{
    public class WorkerService : IWokerService
    {
        private readonly FulfilmentDbContext _dbContext;

        public WorkerService(FulfilmentDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<int> GetCountActiveTasks(int workerId)
        {
            var worker = await _dbContext.Workers.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workerId);
            return worker!.ActiveTasksCount;
        }
    }
}
