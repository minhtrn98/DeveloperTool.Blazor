namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskOrderInfo(
    string PickupTaskId,
    string OrderId,
    bool IsCancel);
