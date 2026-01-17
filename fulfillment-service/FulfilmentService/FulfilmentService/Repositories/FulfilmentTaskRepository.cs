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
        private readonly IWorkerRepository _workerRepository;
        public FulfilmentTaskRepository(FulfilmentDbContext dbContext, IWorkerRepository workerRepository)
        {
            _dbContext = dbContext;
            _workerRepository = workerRepository;
        }
        public async Task<TaskResponse> CreateTask(int orderId)
        {
            var task = await _dbContext.FulfilmentTasks.FirstOrDefaultAsync(f => f.OrderId == orderId);
            if (task is not null)
            {
                return new TaskResponse
                {
                    Id=task.Id,
                    OrderId = orderId,
                    Status = "AlreadyExists"
                };
            }
            var fulfilmentTask = await _dbContext.AddAsync(new FulfillmentTask
            {
                OrderId = orderId,
                Status = "Pending"
            });
            await _dbContext.SaveChangesAsync();
            return new TaskResponse
            {
                Id = fulfilmentTask.Entity.Id,
                OrderId = orderId,
                Status = fulfilmentTask.Entity.Status
            };
        }

        public async Task<TaskResponse> AssignTask(int taskId)
        {
            using var transaction = _dbContext.Database.BeginTransaction();
            var task = await _dbContext.FulfilmentTasks.FirstOrDefaultAsync(f => f.Id == taskId);
            if (task is null)
            {
                throw new Exception("Task not found");
            }
            if (task.WorkerId is not null)
            {
                throw new Exception("Task already assigned");
            }
            int workerId = _dbContext.Cursors.First().Current % 5 + 1;
            workerId = await GetValidWorker(workerId);
            if (workerId == -1)
            {
                throw new Exception("No available workers");
            }
            task.WorkerId = workerId;
            task.Status = "Assigned";
            var cursor = await _dbContext.Cursors.FirstAsync(c => c.Id == 1);
            cursor.Current = workerId;
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return new TaskResponse() { Id = task.Id , OrderId = task.OrderId , Status=task.Status};
        }
        private async Task<int> GetValidWorker(int workerId)
        {
            int steps = 6;
            while (steps-- > 0 && await _workerRepository.GetCountActiveTasks(workerId) == 5)
            {
                workerId = workerId % 5 + 1;
            }
            if (steps <= 0) return -1;
            return workerId;
        }

        public async Task<FulfillmentTask> UpdateTaskStatus(int taskId, UpdateTaskDto model)
        {
            var task = _dbContext.FulfilmentTasks.FirstOrDefault(f => f.Id == taskId);
            if (task is null)
            {
                throw new Exception("Task not found");
            }
            if (task?.WorkerId != model.WorkerId)
            {
                throw new Exception("Worker not assigned to this task");
            }
            if (task.Status == "Completed" || task.Status=="Failed")
            {
                throw new Exception("Task already processed");
            }
            if ( task.Status == "Assigned" && model.Status!="Completed" && model.Status!= "Failed" )
            {
                throw new Exception("Invalid Transition");
            }
            task.Status = model.Status;
            var worker = await _dbContext.Workers.FirstOrDefaultAsync(w => w.Id == model.WorkerId);
            worker!.ActiveTasksCount--;
            await _dbContext.SaveChangesAsync();
            return task;
        }
        public async Task<FulfillmentTask> Get(int taskId)
        {
            var task = await _dbContext.FulfilmentTasks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == taskId);
            return task;
        }
    }
}
