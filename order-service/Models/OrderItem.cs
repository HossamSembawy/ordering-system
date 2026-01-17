namespace OrderService.Models;

    public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }
    public Order Order { get; set; }
}