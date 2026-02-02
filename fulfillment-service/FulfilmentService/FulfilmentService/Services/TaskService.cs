using FulfilmentService.Database;
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using Microsoft.EntityFrameworkCore;
using FulfilmentService.Models;
using System.Threading.Tasks;
using FulfilmentService.Strategies;

namespace FulfilmentService.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAssignmentStrategy _assignmentStrategy;
        public TaskService(IUnitOfWork unitOfWork, IAssignmentStrategy assignmentStrategy)
        {
            _unitOfWork = unitOfWork;
            _assignmentStrategy = assignmentStrategy;
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
                await _unitOfWork.keys.Set($"task:{task.Id}", task.Id.ToString());
                return task;
            }
            catch (Exception ex)
            {
                throw new Exception("Task with this order Id already Exists");
            }
        }

        public async Task<FulfillmentTask> AssignTask(int taskId)
        {
            try
            {
                var task = await _assignmentStrategy.AssignWorker(taskId);
                _unitOfWork.keys.Delete($"task:{task.Id}");
                return task;
            }
            catch(Exception ex)
            {
                throw new Exception("Failed to assign task", ex);
            }
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
        public async Task<List<string>?> GetPendingTasks()
        {
            var tasks = await _unitOfWork.keys.GetList();
            return tasks;
        }
    }
}
