using FulfilmentService.Dtos;

namespace FulfilmentService.Interfaces
{
    public interface IFulfilmentTaskRepository
    {
        Task<TaskResponse> CreateTask(int orderId);
        Task<TaskResponse> AssignTask(int taskId);
        Task<TaskResponse> UpdateTaskStatus(int taskId, UpdateTaskDto model);
    }
}
