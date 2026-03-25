namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record OrderInfo(
    string OrderId,
    decimal Weight,
    decimal W,
    decimal H,
    decimal L,
    DateTime CreatedAt,
    bool HasPickupTask);
