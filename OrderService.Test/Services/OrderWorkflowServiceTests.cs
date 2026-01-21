using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;
using OrderService.Test.Data;

namespace OrderService.Test.Services
{
	public class OrderWorkflowServiceTests
	{
        [Fact]
        public async Task PlaceOrder_HappyPath_UpdatesInventoryAndCompletesOrder()
        {
            var (context, connection) = DbContextHelper.GetSqliteInMemoryDbContext();
            await using var dbContext = context;
            using var _ = connection;

            dbContext.Inventories.Add(new Inventory
            {
                ProductId = 1,
                AvailableQty = 10
            });
            await dbContext.SaveChangesAsync();

            var fulfillmentClient = new TestFulfillmentClient();
            var service = new OrderWorkflowService(dbContext, fulfillmentClient);

            var order = await service.PlaceOrderAsync(
                userId: 101,
                idempotencyKey: Guid.NewGuid().ToString(),
                items: new List<OrderItemRequest>
                {
            new() { ProductId = 1, Qty = 2 }
                });

            // Order created
            Assert.Equal(OrderStatus.Pending, order.Status);

            // Fire-and-forget task created
            Assert.Single(fulfillmentClient.CreatedTasks);
            Assert.Equal(order.OrderId, fulfillmentClient.CreatedTasks[0].OrderId);

            // Inventory deducted
            var inventory = await dbContext.Inventories.SingleAsync(i => i.ProductId == 1);
            Assert.Equal(8, inventory.AvailableQty);

            // Fulfillment lifecycle
            var assigned = await service.ApplyFulfillmentUpdateAsync(
                order.OrderId,
                FulfillmentTaskStatus.Assigned,
                workerId: 1);

            Assert.True(assigned);

            var completed = await service.ApplyFulfillmentUpdateAsync(
                order.OrderId,
                FulfillmentTaskStatus.Completed,
                workerId: 1);

            Assert.True(completed);

            var confirmedOrder =
                await dbContext.Orders.SingleAsync(o => o.OrderId == order.OrderId);

            Assert.Equal(OrderStatus.Completed, confirmedOrder.Status);
        }


        [Fact]
        public async Task PlaceOrder_InsufficientStock_DoesNotCreateOrder()
        {
            var (context, connection) = DbContextHelper.GetSqliteInMemoryDbContext();
            await using var dbContext = context;
            using var _ = connection;

            dbContext.Inventories.Add(new Inventory
            {
                ProductId = 1,
                AvailableQty = 1
            });
            await dbContext.SaveChangesAsync();

            var fulfillmentClient = new TestFulfillmentClient();
            var service = new OrderWorkflowService(dbContext, fulfillmentClient);

            var ex = await Assert.ThrowsAsync<OrderPlacementException>(() =>
                service.PlaceOrderAsync(
                    userId: 201,
                    idempotencyKey: Guid.NewGuid().ToString(),
                    items: new List<OrderItemRequest>
                    {
                new() { ProductId = 1, Qty = 2 }
                    }));

            Assert.Equal("INSUFFICIENT_STOCK", ex.Code);

            Assert.Empty(dbContext.Orders);

            var inventory =
                await dbContext.Inventories.SingleAsync(i => i.ProductId == 1);

            Assert.Equal(1, inventory.AvailableQty);

            Assert.Empty(fulfillmentClient.CreatedTasks);
        }


        [Fact]
        public async Task ApplyFulfillmentUpdate_Rejected_DeletesOrder()
        {
            var (context, connection) = DbContextHelper.GetSqliteInMemoryDbContext();
            await using var dbContext = context;
            using var _ = connection;

            dbContext.Inventories.Add(new Inventory
            {
                ProductId = 1,
                AvailableQty = 5
            });
            await dbContext.SaveChangesAsync();

            var fulfillmentClient = new TestFulfillmentClient();
            var service = new OrderWorkflowService(dbContext, fulfillmentClient);

            var order = await service.PlaceOrderAsync(
                userId: 301,
                idempotencyKey: Guid.NewGuid().ToString(),
                items: new List<OrderItemRequest>
                {
            new() { ProductId = 1, Qty = 1 }
                });

            var rejected = await service.ApplyFulfillmentUpdateAsync(
                order.OrderId,
                FulfillmentTaskStatus.Rejected,
                workerId: 2);

            Assert.True(rejected);
            Assert.Empty(dbContext.Orders);
        }

    }
}
