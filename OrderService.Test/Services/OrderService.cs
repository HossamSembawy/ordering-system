using Microsoft.EntityFrameworkCore;
using OrderService.contacts;
using OrderService.Models;
using OrderService.Services;
using OrderService.Test.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Test.Services
{
    public class OrderService
    {
        [Fact]
        public async Task UpdateAsync_Should_Update_Order_Status()
        {
            // Arrange
            var context = DbContextHelper.GetInMemoryDbContext();
            IOrderService service = new OrdersService(context);

            var order = new Order
            {
                OrderId = 1,
                UserId = 1,
                Status = "Pending"
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var updatedOrder = new Order
            {
                OrderId = 1,
                Status = "Completed"
            };

            // Act
            var result = await service.UpdateAsync(order.OrderId, updatedOrder);

            var savedOrder = context.Orders.First(o => o.OrderId == order.OrderId);

            // Assert
            Assert.True(result);
            Assert.Equal("Completed", savedOrder.Status);
            Assert.True(savedOrder.UpdatedAt > savedOrder.CreatedAt);
        }


    }
}
