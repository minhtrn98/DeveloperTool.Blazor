namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskOrderItemInfo(
    string OrderId,
    string OrderItemId,
    decimal Weight,
    decimal W,
    decimal H,
    decimal L,
    short Status,
    bool HasPickupTask)
{
    public bool IsCancel => Status == 9;
}
