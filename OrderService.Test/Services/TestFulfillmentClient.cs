using OrderService.contacts;
using OrderService.Models;

namespace OrderService.Test.Services
{
    public class TestFulfillmentClient : IFulfillmentClient
    {
        public List<FulfillmentTask> CreatedTasks { get; } = new();

        public Task<FulfillmentTask> CreateTaskAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var task = new FulfillmentTask
            {
                OrderId = orderId,
                Status = FulfillmentTaskStatus.Created
            };

            CreatedTasks.Add(task);
            return Task.FromResult(task);
        }
    }
}
