using Microsoft.Data.Sqlite;
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

        public OrderWorkflowService(
            OrderDbContext context,
            IFulfillmentClient fulfillmentClient)
        {
            _context = context;
            _fulfillmentClient = fulfillmentClient;
        }

        public async Task<Order> PlaceOrderAsync(
            int userId,
            string idempotencyKey,
            IReadOnlyCollection<OrderItemRequest> items,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new OrderPlacementException(
                    "INVALID_REQUEST", "Idempotency key is required.");

            if (items == null || items.Count == 0)
                throw new OrderPlacementException(
                    "INVALID_REQUEST", "Order must contain at least one item.");

            if (items.Any(i => i.Qty <= 0))
                throw new OrderPlacementException(
                    "INVALID_REQUEST", "Order items must have a quantity greater than zero.");

            // 1️⃣ Idempotency check (fast path)
            var existing = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.UserId == userId &&
                    o.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (existing != null)
                return existing;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            // 2️⃣ Optimistic stock deduction (atomic)
            foreach (var item in items)
            {
                var rowsAffected =
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                    UPDATE Inventories
                    SET AvailableQty = AvailableQty - {item.Qty},
                        UpdatedAt = {DateTime.UtcNow}
                    WHERE ProductId = {item.ProductId}
                      AND AvailableQty >= {item.Qty}
                    """,
                        cancellationToken);

                if (rowsAffected == 0)
                {
                    throw new OrderPlacementException(
                        "INSUFFICIENT_STOCK",
                        $"Not enough stock for product {item.ProductId}");
                }
            }

            // 3️⃣ Persist order
            var order = new Order
            {
                UserId = userId,
                IdempotencyKey = idempotencyKey,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Qty = i.Qty
                }).ToList()
            };

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();

                var duplicate = await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.UserId == userId &&
                        o.IdempotencyKey == idempotencyKey,
                        cancellationToken);

                if (duplicate != null)
                    return duplicate;

                throw;
            }

            // 🔥 Raw SQL invalidates tracking
            _context.ChangeTracker.Clear();

            // 4️⃣ Fire-and-forget fulfillment task
            _ = Task.Run(() =>
                _fulfillmentClient.CreateTaskAsync(
                    order.OrderId,
                    CancellationToken.None));

            return order;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqliteException sqliteException
                && sqliteException.SqliteErrorCode == 19;
        }

        public async Task<bool> ApplyFulfillmentUpdateAsync(
            int orderId,
            string taskStatus,
            int? workerId,
            CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

            if (order == null)
                return false;

            switch (taskStatus)
            {
                case FulfillmentTaskStatus.Assigned:
                case FulfillmentTaskStatus.InProgress:
                    order.Status = OrderStatus.Pending;
                    order.UpdatedAt = DateTime.UtcNow;
                    break;

                case FulfillmentTaskStatus.Completed:
                    order.Status = OrderStatus.Completed;
                    order.UpdatedAt = DateTime.UtcNow;
                    break;

                case FulfillmentTaskStatus.Rejected:
                    _context.Orders.Remove(order);
                    break;

                default:
                    throw new OrderPlacementException(
                        "INVALID_STATUS",
                        $"Unsupported fulfillment status: {taskStatus}");
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

}
