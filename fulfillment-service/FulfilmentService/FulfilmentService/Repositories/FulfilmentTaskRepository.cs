using FulfilmentService.Database;
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using Microsoft.EntityFrameworkCore;
using FulfilmentService.Models;

namespace FulfilmentService.Repositories
{
    public class FulfilmentTaskRepository : IFulfilmentTaskRepository
    {
        private readonly FulfilmentDbContext _dbContext;

        public FulfilmentTaskRepository(FulfilmentDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<TaskResponse> CreateTask(int orderId)
        {
            var task = await _dbContext.FulfilmentTasks.AnyAsync(f => f.OrderId == orderId);
            if (task)
            {
                return new TaskResponse
                {
                    OrderId = orderId,
                    Status = Status.Created
                };
            }
            var fulfilmentTask = await _dbContext.AddAsync(new FulfilmentTask
            {
                OrderId = orderId,
                Status = Status.Pending
            });
            await _dbContext.SaveChangesAsync();
            return new TaskResponse
            {
                OrderId = orderId,
                Status = fulfilmentTask.Entity.Status
            };
        }

        public Task<TaskResponse> UpdateTaskStatus(int taskId, int workerId)
        {
            throw new NotImplementedException();
        }
    }
}
