using OrderService.contacts;
using OrderService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Test.Data
{
    public class FakeFulfillmentClient : IFulfillmentClient
    {
        public Task<FulfillmentTask> CreateTaskAsync(
            int orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new FulfillmentTask
            {
                OrderId = orderId,
                Status = FulfillmentTaskStatus.Created,
            });
        }
    }

}
