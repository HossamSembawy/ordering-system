using FulfilmentService.Dtos;
using FulfilmentService.Models;

namespace FulfilmentService.ExternalClients
{
    public interface IOrderServiceClient
    {
        Task<HttpResponseMessage> UpdateOrderStatus(FulfillmentTask task, CancellationToken cancellationToken = default);
    }
}
