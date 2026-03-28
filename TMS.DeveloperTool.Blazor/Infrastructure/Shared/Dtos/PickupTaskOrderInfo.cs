namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskOrderInfo(
    string PickupTaskId,
    string OrderId,
    short Status)
{
    public bool IsCancel => Status == 9;
}
