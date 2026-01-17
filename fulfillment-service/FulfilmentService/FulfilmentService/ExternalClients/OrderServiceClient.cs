using FulfilmentService.Dtos;
using FulfilmentService.Models;

namespace FulfilmentService.ExternalClients
{
    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _httpClient;

        public OrderServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<HttpResponseMessage> UpdateOrderStatus(FulfillmentTask task, CancellationToken cancellationToken = default)
        {
            var request = new OrderUpdateRequest
            {
                OrderId = task.OrderId,
                Status = task.Status,
                WorkerId = task.WorkerId
            };
            var response = await _httpClient.PostAsJsonAsync($"/orders/{request.OrderId}/fulfillment", request, cancellationToken);
            response.EnsureSuccessStatusCode();            
            return response;
        }
        
    }
}
