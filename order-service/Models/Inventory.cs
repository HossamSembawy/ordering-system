namespace OrderService.Models;

public class Inventory
{
    public int ProductId { get; set; }
    public int AvailableQty { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}