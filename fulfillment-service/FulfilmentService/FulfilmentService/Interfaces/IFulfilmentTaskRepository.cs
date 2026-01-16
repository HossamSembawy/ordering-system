using FulfilmentService.Dtos;

namespace FulfilmentService.Interfaces
{
    public interface IFulfilmentTaskRepository
    {
        Task<TaskResponse> CreateTask(int orderId);
        Task<TaskResponse> UpdateTaskStatus(int taskId, int workerId);
    }
}
