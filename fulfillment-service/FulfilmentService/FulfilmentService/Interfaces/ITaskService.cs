using FulfilmentService.Dtos;
using FulfilmentService.Models;

namespace FulfilmentService.Interfaces
{
    public interface ITaskService
    {
        Task<FulfillmentTask> CreateTask(int orderId);
        Task<FulfillmentTask> AssignTask(int taskId);
        Task<FulfillmentTask> UpdateTaskStatus(int taskId, UpdateTaskDto model);
        Task<FulfillmentTask> Get(int taskId);
        Task<FulfillmentTask> GetByOrderId(int orderId);
        Task<List<FulfillmentTask>?> GetPendingTasks();
    }
}
