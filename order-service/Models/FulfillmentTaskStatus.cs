namespace OrderService.Models;

public static class FulfillmentTaskStatus
{
	public const string Created = "CREATED";
	public const string Assigned = "ASSIGNED";
	public const string InProgress = "IN_PROGRESS";
	public const string Completed = "COMPLETED";
	public const string Rejected = "REJECTED";
}
