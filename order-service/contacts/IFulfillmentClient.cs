using OrderService.Models;

namespace OrderService.contacts
{
	public interface IFulfillmentClient
	{
		Task<FulfillmentTask> CreateTaskAsync(int orderId, CancellationToken cancellationToken = default);
	}
}
