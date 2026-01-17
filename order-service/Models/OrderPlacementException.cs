namespace OrderService.Models;

public class OrderPlacementException : Exception
{
    public OrderPlacementException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
