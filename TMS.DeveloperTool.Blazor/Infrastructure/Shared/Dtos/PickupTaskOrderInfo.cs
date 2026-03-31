using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskOrderInfo(
    string PickupTaskId,
    string OrderId,
    PickupTaskOrderStatus Status)
{
    public bool IsCancel => Status == PickupTaskOrderStatus.Cancelled;
}
