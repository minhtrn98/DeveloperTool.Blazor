namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed record PickupTaskEventOrderDto(
    string OrderId,
    string CreatedAt,
    decimal Weight,
    decimal L,
    decimal H,
    decimal W,
    bool HasPickupTask,
    IReadOnlyList<PickupTaskEventOrderItemDto> Items);

public sealed record PickupTaskEventOrderItemDto(
    string OrderId,
    string OrderItemId,
    decimal Weight,
    decimal L,
    decimal H,
    decimal W,
    bool HasPickupTask);