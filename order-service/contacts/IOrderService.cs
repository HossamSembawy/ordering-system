using OrderService.Models;

namespace OrderService.contacts
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(int orderId);
        Task<Order> CreateAsync(Order order);
        Task<bool> UpdateAsync(int orderId, Order order);
        Task<bool> DeleteAsync(int orderId);
    }
}
