using FulfilmentService.Models;

namespace FulfilmentService.Strategies
{
    public interface IAssignmentStrategy
    {
        public Task<FulfillmentTask?> AssignWorker(int taskId);
    }
}
