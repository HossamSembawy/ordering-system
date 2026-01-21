namespace OrderService.Models;

public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

