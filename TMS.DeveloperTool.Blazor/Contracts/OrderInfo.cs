namespace TMS.DeveloperTool.Blazor.Contracts;

public sealed record OrderInfo(string OrderId, DateTime CreatedAt, bool HasPickupTask);
public sealed record OrderItemInfo(string OrderItemId, decimal Weight, decimal W, decimal H, decimal L, bool HasPickupTask);