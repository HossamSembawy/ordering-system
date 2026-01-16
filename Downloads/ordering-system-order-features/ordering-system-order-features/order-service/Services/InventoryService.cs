using Microsoft.EntityFrameworkCore;
using OrderService.contacts;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly OrderDbContext _context;

        public InventoryService(OrderDbContext context)
        {
            _context = context;
        }

        // Get all inventory items
        public async Task<IEnumerable<Inventory>> GetAllAsync()
        {
            return await _context.Inventories
                .AsNoTracking()
                .ToListAsync();
        }

        // Get a single inventory item by ProductId
        public async Task<Inventory?> GetByIdAsync(int productId)
        {
            return await _context.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId);
        }

        // Create a new inventory item
        public async Task<Inventory> CreateAsync(Inventory inventory)
        {
            inventory.UpdatedAt = DateTime.UtcNow;
            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();
            return inventory;
        }

        // Update an existing inventory item
        public async Task<bool> UpdateAsync(Inventory inventory)
        {
            var existing = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == inventory.ProductId);

            if (existing == null) return false;

            existing.AvailableQty = inventory.AvailableQty;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.Inventories.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // Delete an inventory item
        public async Task<bool> DeleteAsync(int productId)
        {
            var existing = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId);

            if (existing == null) return false;

            _context.Inventories.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
