using FulfilmentService.Interfaces;
using FulfilmentService.Models;

namespace FulfilmentService.Strategies
{
    public class RoundRobinStrategy : IAssignmentStrategy
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoundRobinStrategy(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<FulfillmentTask> AssignWorker(int taskId)
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
                var worker = await _unitOfWork.WorkerRepository.GetWithLocking(workerId, "Workers");
                if (worker!.ActiveTasksCount < MaxTasks)
                {
                    return workerId;
                }
                workerId = workerId % 5 + 1;
            }
            if (steps <= 0) return -1;
            return workerId;
        }
    }
}
