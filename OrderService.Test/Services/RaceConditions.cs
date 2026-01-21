using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
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
    public class RaceConditions
    {
        [Fact]
        public async Task Concurrent_orders_should_not_oversell_inventory()
        {
            const string connectionString =
               "DataSource=order-race;Mode=Memory;Cache=Shared";

            var keepAlive = new SqliteConnection(connectionString);


            await keepAlive.OpenAsync();

            // 🔥 schema + seed مرة واحدة
            await using (var setup = TestDbFactory.CreateContext(keepAlive))
            {
                await setup.Database.EnsureCreatedAsync();
                await SeedInventoryAsync(setup);
            }

            // ✅ LOCAL FUNCTION
            async Task Run(int userId)
            {
                await using var connection =
                    new SqliteConnection(connectionString);

                await connection.OpenAsync();

                await using var ctx = TestDbFactory.CreateContext(connection);
                await ctx.Database.EnsureCreatedAsync(); // 🔥 مهم

                var svc = new OrderWorkflowService(ctx, new FakeFulfillmentClient());

                try
                {
                    await svc.PlaceOrderAsync(
                        userId,
                        Guid.NewGuid().ToString(),
                        new[]
                        {
                    new OrderItemRequest { ProductId = 1, Qty = 3 }
                        });
                }
                catch (OrderPlacementException ex)
                    when (ex.Code == "INSUFFICIENT_STOCK")
                {
                    // expected
                }
            }

            // 🔥 concurrency
            await Task.WhenAll(
                Task.Run(() => Run(1)),
                Task.Run(() => Run(2)));

            await using var assertCtx = TestDbFactory.CreateContext(keepAlive);

            var orders = await assertCtx.Orders.ToListAsync();
            var inventory = await assertCtx.Inventories.SingleAsync();

            Assert.Single(orders);
            Assert.Equal(2, inventory.AvailableQty);
        }






        [Fact]
        public async Task Concurrent_requests_with_same_idempotency_key_should_create_one_order()
        {
            const string connectionString =
                "DataSource=order-idempotency;Mode=Memory;Cache=Shared";

            var keepAlive = new SqliteConnection(connectionString);
            await keepAlive.OpenAsync();


            await using (var setup = TestDbFactory.CreateContext(keepAlive))
            {
                await setup.Database.EnsureCreatedAsync();
                await SeedInventoryAsync(setup);
            }

            const string key = "same-key-123";

            async Task Run()
            {
                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                await using var ctx = TestDbFactory.CreateContext(connection);

                var svc = new OrderWorkflowService(ctx, new FakeFulfillmentClient());

                await svc.PlaceOrderAsync(
                    1,
                    key, // 🔑 same idempotency key
                    new[]
                    {
                new OrderItemRequest { ProductId = 1, Qty = 2 }
                    });
            }

            var t1 = Task.Run(Run);
            var t2 = Task.Run(Run);

            await Task.WhenAll(t1, t2);

            await using var assertCtx = TestDbFactory.CreateContext(keepAlive);

            var orders = await assertCtx.Orders.ToListAsync();
            var inventory = await assertCtx.Inventories.FirstAsync();

            Assert.Single(orders);
            Assert.Equal(3, inventory.AvailableQty); // 5 - 2
        }




        private static async Task SeedInventoryAsync(OrderDbContext context)
        {
            context.Inventories.Add(new Inventory
            {
                ProductId = 1,
                AvailableQty = 5,
                UpdatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

    }
}
