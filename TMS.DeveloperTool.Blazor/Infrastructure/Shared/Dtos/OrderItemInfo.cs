namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record OrderItemInfo(
    string OrderId,
    string OrderItemId,
    decimal Weight,
    decimal W,
    decimal H,
    decimal L,
    bool HasPickupTask);
