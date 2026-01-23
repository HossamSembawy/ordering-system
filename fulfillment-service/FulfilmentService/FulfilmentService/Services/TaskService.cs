using FulfilmentService.Database;
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using Microsoft.EntityFrameworkCore;
using FulfilmentService.Models;
using System.Threading.Tasks;

namespace FulfilmentService.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TaskService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<FulfillmentTask> CreateTask(int orderId)
        {
            try
            {
                var fulfilmentTask = new FulfillmentTask
                {
                    OrderId = orderId,
                    Status = "Pending"
                };
                var task = await _unitOfWork.taskRepository.AddAsync(fulfilmentTask);
                return task;
            }
            catch (Exception ex)
            {
                throw new Exception("Task with this order Id already Exists");
            }
        }

        public async Task<FulfillmentTask> AssignTask(int taskId)
        {
            using var transaction = await _unitOfWork.BeginTransaction(false);
            try
            {
                var task = await _unitOfWork.taskRepository.GetByCondition(f => f.Id == taskId);
                if (task is null)
                {
                    throw new Exception("Task not found");
                }
                if (task.WorkerId is not null)
                {
                    throw new Exception("Task already assigned");
                }
                var cursorWorkerId = await _unitOfWork.CursorRepository.GetByCondition(c => c.Id == 1);
                int nextWorkerId = cursorWorkerId!.Current % 5 + 1;
                int workerId = await GetValidWorker(nextWorkerId);
                if (workerId == -1)
                {
                    throw new Exception("No available workers");
                }
                task.WorkerId = workerId;
                task.Status = "ASSIGNED";
                cursorWorkerId.Current = workerId;
                var worker = await _unitOfWork.WorkerRepository.GetWithLocking(workerId, "Workers");
                worker!.ActiveTasksCount++;
                await _unitOfWork.taskRepository.UpdateAsync(task);
                await _unitOfWork.WorkerRepository.UpdateAsync(worker);
                await _unitOfWork.CursorRepository.UpdateAsync(cursorWorkerId);
                await _unitOfWork.SaveChanges();
                await transaction.CommitAsync();
                return task;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Failed to assign a task", ex);
            }
        }
        private async Task<int> GetValidWorker(int workerId)
        {
            const int MaxTasks = 5;
            int steps = 6;
            while (steps-- > 0)
            {
                var worker = await _unitOfWork.WorkerRepository.GetWithLocking(workerId,"Workers");
                if (worker!.ActiveTasksCount < MaxTasks)
                {
                    return workerId;
                }
                workerId = workerId % 5 + 1;
            }
            if (steps <= 0) return -1;
            return workerId;
        }

        public async Task<FulfillmentTask> UpdateTaskStatus(int taskId, UpdateTaskDto model)
        {
            using var transaciton = await _unitOfWork.BeginTransaction(false);
            try
            {
                var task = await Get(taskId);
                if (task is null)
                {
                    throw new Exception("Task not found");
                }
                if (task?.WorkerId != model.WorkerId)
                {
                    throw new Exception("Worker not assigned to this task");
                }
                if (task.Status == "COMPLETED" || task.Status == "FAILED")
                {
                    throw new Exception("Task already processed");
                }
                if (task.Status == "ASSIGNED" && model.Status != "COMPLETED" && model.Status != "FAILED")
                {
                    throw new Exception("Invalid Transition");
                }
                task.Status = model.Status;
                var worker = await _unitOfWork.WorkerRepository.GetWithLocking(task.WorkerId.Value, "Workers");
                worker!.ActiveTasksCount--;
                await _unitOfWork.SaveChanges();
                await transaciton.CommitAsync();
                return task;
            }catch (Exception ex)
            {
                await transaciton.RollbackAsync();
                throw new Exception("Failed to update task status",ex);
            }
        }
        public async Task<FulfillmentTask?> Get(int taskId)
        {
            var task = await _unitOfWork.taskRepository.GetByCondition(f => f.Id == taskId);
            return task;
        }

        public async Task<FulfillmentTask> GetByOrderId(int orderId)
        {
            var task = await _unitOfWork.taskRepository.GetByCondition(f => f.OrderId == orderId);
            return task;
        }
        public async Task<List<FulfillmentTask>?> GetPendingTasks()
        {
            var tasks = await _unitOfWork.taskRepository.GetListByCondition(f => f.Status == "Pending");
            return tasks;
        }
    }
}
