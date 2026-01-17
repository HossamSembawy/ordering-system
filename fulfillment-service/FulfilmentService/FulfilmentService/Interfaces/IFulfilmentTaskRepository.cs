using FulfilmentService.Dtos;
using FulfilmentService.Models;

namespace FulfilmentService.Interfaces
{
    public interface IFulfilmentTaskRepository
    {
        Task<TaskResponse> CreateTask(int orderId);
        Task<TaskResponse> AssignTask(int taskId);
        Task<FulfillmentTask> UpdateTaskStatus(int taskId, UpdateTaskDto model);
        Task<FulfillmentTask> Get(int taskId);
    }
}
