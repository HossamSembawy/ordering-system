using Microsoft.EntityFrameworkCore;
using OrderService.contacts;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly OrderDbContext _context;

        public OrderItemService(OrderDbContext context)
        {
            _context = context;
        }

        // Get all OrderItems
        public async Task<IEnumerable<OrderItem>> GetAllAsync()
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .AsNoTracking()
                .ToListAsync();
        }

        // Get OrderItem by Id
        public async Task<OrderItem?> GetByIdAsync(int id)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .AsNoTracking()
                .FirstOrDefaultAsync(oi => oi.Id == id);
        }

        // Create a new OrderItem
        public async Task<OrderItem> CreateAsync(OrderItem item)
        {
            _context.OrderItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

    }
}
