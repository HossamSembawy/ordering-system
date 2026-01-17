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
            new Inventory { AvailableQty = 100 },
            new Inventory { AvailableQty = 50 },
            new Inventory { AvailableQty = 75 },
            new Inventory { AvailableQty = 200 },
            new Inventory { AvailableQty = 30 }
        };

        context.Inventories.AddRange(inventoryItems);
        context.SaveChanges();
    }
}