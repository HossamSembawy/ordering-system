using OrderService.contacts;
using OrderService.Models;
using OrderService.Services;
using OrderService.Test.Data;

namespace OrderService.Test.Services
{
    public class OrderService
    {
        [Fact]
        public async Task UpdateAsync_Should_Update_Order_Status()
        {
            // Arrange
            var (context, connection) = DbContextHelper.GetSqliteInMemoryDbContext();
            await using var dbContext = context;
            using var _ = connection;
            IOrderService service = new OrdersService(dbContext);

            var order = new Order
            {
                OrderId = 1,
                UserId = 1,
                Status = "Pending",
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            var updatedOrder = new Order
            {
                OrderId = 1,
                Status = "Completed"
            };

            // Act
            var result = await service.UpdateAsync(order.OrderId, updatedOrder);

            var savedOrder = dbContext.Orders.First(o => o.OrderId == order.OrderId);

            // Assert
            Assert.True(result);
            Assert.Equal("Completed", savedOrder.Status);
            Assert.True(savedOrder.UpdatedAt > savedOrder.CreatedAt);
        }


    }
}
