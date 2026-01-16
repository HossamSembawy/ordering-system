using System.Net.Http.Json;
using OrderService.contacts;
using OrderService.Models;

namespace OrderService.Services
{
    public class HttpFulfillmentClient : IFulfillmentClient
    {
        private readonly HttpClient _httpClient;

        public HttpFulfillmentClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FulfillmentTask> CreateTaskAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("/tasks", new { orderId }, cancellationToken);
            response.EnsureSuccessStatusCode();

            var task = await response.Content.ReadFromJsonAsync<FulfillmentTask>(cancellationToken: cancellationToken);
            if (task == null)
            {
                throw new OrderPlacementException("FULFILLMENT_RESPONSE_INVALID", "Fulfillment service returned an empty response.");
            }

            return task;
        }
    }
}
