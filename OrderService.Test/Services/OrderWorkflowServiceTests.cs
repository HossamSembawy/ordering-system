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
			dbContext.Inventories.Add(new Inventory { ProductId = 1, AvailableQty = 10 });
			await dbContext.SaveChangesAsync();

			var fulfillmentClient = new TestFulfillmentClient();
			var service = new OrderWorkflowService(dbContext, fulfillmentClient);

			var order = await service.PlaceOrderAsync(101, new List<OrderItemRequest>
			{
				new() { ProductId = 1, Qty = 2 }
			});

			Assert.Equal(OrderStatus.Pending, order.Status);
			Assert.Single(fulfillmentClient.CreatedTasks);
			Assert.Equal(order.OrderId, fulfillmentClient.CreatedTasks[0].OrderId);

			var inventory = await dbContext.Inventories.SingleAsync(i => i.ProductId == 1);
			Assert.Equal(8, inventory.AvailableQty);

			var assigned = await service.ApplyFulfillmentUpdateAsync(order.OrderId, FulfillmentTaskStatus.Assigned, "worker-1");
			Assert.True(assigned);
			var assignedOrder = await dbContext.Orders.SingleAsync(o => o.OrderId == order.OrderId);
			Assert.Equal(OrderStatus.Pending, assignedOrder.Status);

			var completed = await service.ApplyFulfillmentUpdateAsync(order.OrderId, FulfillmentTaskStatus.Completed, "worker-1");
			Assert.True(completed);
			var confirmedOrder = await dbContext.Orders.SingleAsync(o => o.OrderId == order.OrderId);
			Assert.Equal(OrderStatus.Completed, confirmedOrder.Status);
		}

		[Fact]
		public async Task PlaceOrder_InsufficientStock_DoesNotCreateOrder()
		{
			var (context, connection) = DbContextHelper.GetSqliteInMemoryDbContext();
			await using var dbContext = context;
			using var _ = connection;
			dbContext.Inventories.Add(new Inventory { ProductId = 1, AvailableQty = 1 });
			await dbContext.SaveChangesAsync();

			var fulfillmentClient = new TestFulfillmentClient();
			var service = new OrderWorkflowService(dbContext, fulfillmentClient);

			var exception = await Assert.ThrowsAsync<OrderPlacementException>(() =>
				service.PlaceOrderAsync(201, new List<OrderItemRequest>
				{
					new() { ProductId = 1, Qty = 2 }
				}));

			Assert.Equal("INSUFFICIENT_STOCK", exception.Code);
			Assert.Empty(dbContext.Orders);
			var inventory = await dbContext.Inventories.SingleAsync(i => i.ProductId == 1);
			Assert.Equal(1, inventory.AvailableQty);
			Assert.Empty(fulfillmentClient.CreatedTasks);
		}

		[Fact]
		public async Task ApplyFulfillmentUpdate_Rejected_DeletesOrder()
		{
			var (context, connection) = DbContextHelper.GetSqliteInMemoryDbContext();
			await using var dbContext = context;
			using var _ = connection;
			dbContext.Inventories.Add(new Inventory { ProductId = 1, AvailableQty = 5 });
			await dbContext.SaveChangesAsync();

			var fulfillmentClient = new TestFulfillmentClient();
			var service = new OrderWorkflowService(dbContext, fulfillmentClient);

			var order = await service.PlaceOrderAsync(301, new List<OrderItemRequest>
			{
				new() { ProductId = 1, Qty = 1 }
			});

			var rejected = await service.ApplyFulfillmentUpdateAsync(order.OrderId, FulfillmentTaskStatus.Rejected, "worker-2");
			Assert.True(rejected);
			Assert.Empty(dbContext.Orders);
		}
	}
}
