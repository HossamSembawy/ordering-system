using Microsoft.EntityFrameworkCore;
using OrderService.contacts;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services
{
	public class OrderWorkflowService
	{
		private readonly OrderDbContext _context;
		private readonly IFulfillmentClient _fulfillmentClient;

		public OrderWorkflowService(OrderDbContext context, IFulfillmentClient fulfillmentClient)
		{
			_context = context;
			_fulfillmentClient = fulfillmentClient;
		}

		public async Task<Order> PlaceOrderAsync(
			int userId,
			IReadOnlyCollection<OrderItemRequest> items,
			CancellationToken cancellationToken = default)
		{
			if (items == null || items.Count == 0)
			{
				throw new OrderPlacementException("INVALID_REQUEST", "Order must contain at least one item.");
			}

			if (items.Any(item => item.Qty <= 0))
			{
				throw new OrderPlacementException("INVALID_REQUEST", "Order items must have a quantity greater than zero.");
			}

			var productIds = items.Select(item => item.ProductId).Distinct().ToList();

			await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

			var inventories = await _context.Inventories
				.Where(inventory => productIds.Contains(inventory.ProductId))
				.ToListAsync(cancellationToken);

			var missingProductIds = productIds.Except(inventories.Select(i => i.ProductId)).ToList();
			if (missingProductIds.Count > 0)
			{
				throw new OrderPlacementException("PRODUCT_NOT_FOUND", $"Unknown product IDs: {string.Join(", ", missingProductIds)}");
			}

			foreach (var item in items)
			{
				var inventory = inventories.First(i => i.ProductId == item.ProductId);
				if (inventory.AvailableQty < item.Qty)
				{
					throw new OrderPlacementException(
						"INSUFFICIENT_STOCK",
						$"Not enough stock for product {item.ProductId} (requested: {item.Qty}, available: {inventory.AvailableQty}).");
				}
			}

			foreach (var item in items)
			{
				var inventory = inventories.First(i => i.ProductId == item.ProductId);
				inventory.AvailableQty -= item.Qty;
				inventory.UpdatedAt = DateTime.UtcNow;
			}

			var order = new Order
			{
				UserId = userId,
				Status = OrderStatus.Pending,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				Items = items.Select(item => new OrderItem
				{
					ProductId = item.ProductId,
					Qty = item.Qty
				}).ToList()
			};

			_context.Orders.Add(order);
			await _context.SaveChangesAsync(cancellationToken);

			await _fulfillmentClient.CreateTaskAsync(order.OrderId, cancellationToken);

			await transaction.CommitAsync(cancellationToken);

			return order;
		}

		public async Task<bool> ApplyFulfillmentUpdateAsync(
			int orderId,
			string taskStatus,
			int? workerId,
			CancellationToken cancellationToken = default)
		{
			var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
			if (order == null)
			{
				return false;
			}

			switch (taskStatus)
			{
				case FulfillmentTaskStatus.Assigned:
				case FulfillmentTaskStatus.InProgress:
					order.Status = OrderStatus.Pending;
					order.UpdatedAt = DateTime.UtcNow;
					await _context.SaveChangesAsync(cancellationToken);
					return true;
				case FulfillmentTaskStatus.Completed:
					order.Status = OrderStatus.Completed;
					order.UpdatedAt = DateTime.UtcNow;
					await _context.SaveChangesAsync(cancellationToken);
					return true;
				case FulfillmentTaskStatus.Rejected:
					_context.Orders.Remove(order);
					await _context.SaveChangesAsync(cancellationToken);
					return true;
				default:
					throw new OrderPlacementException("INVALID_STATUS", $"Unsupported fulfillment status: {taskStatus}.");
			}
		}
	}
}
