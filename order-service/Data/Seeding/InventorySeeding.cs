using OrderService.Models;

namespace OrderService.Data.Seeding;

public static class InventorySeeding
{
    public static void SeedInventory(OrderDbContext context)
    {
        if (context.Inventories.Any())
        {
            return; // Already seeded
        }

        var inventoryItems = new List<Inventory>
        {
            new Inventory { ProductId = 1, AvailableQty = 100 },
            new Inventory { ProductId = 2, AvailableQty = 50 },
            new Inventory { ProductId = 3, AvailableQty = 75 },
            new Inventory { ProductId = 4, AvailableQty = 200 },
            new Inventory { ProductId = 5, AvailableQty = 30 }
        };

        context.Inventories.AddRange(inventoryItems);
        context.SaveChanges();
    }
}