namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record OrderDto(
    string OrderId,
    decimal Weight,
    decimal W,
    decimal H,
    decimal L,
    DateTime CreatedAt,
    bool HasPickupTask);
