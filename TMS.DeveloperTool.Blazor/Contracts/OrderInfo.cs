namespace TMS.DeveloperTool.Blazor.Contracts;

public sealed record OrderInfo(
    string OrderId,
    decimal Weight,
    decimal W,
    decimal H,
    decimal L,
    DateTime CreatedAt,
    bool HasPickupTask);

public sealed record OrderItemInfo(string OrderId, string OrderItemId, decimal Weight, decimal W, decimal H, decimal L, bool HasPickupTask);