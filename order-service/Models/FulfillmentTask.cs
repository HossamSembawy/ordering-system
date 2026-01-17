namespace OrderService.Models;

public class FulfillmentTask
{
    public Guid TaskId { get; set; } = Guid.NewGuid();
    public int OrderId { get; set; }
    public string Status { get; set; } = FulfillmentTaskStatus.Created;
    public string? WorkerId { get; set; }
}
