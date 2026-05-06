namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

public sealed record ConfirmArrivalRequest(
    string[] PickupTaskIds,
    Guid ReasonId,
    int InputDistance
)
{
    public static readonly Guid DefaultReasonId = Guid.Parse("00000000-0000-0000-0000-000000000025");
    public const int DefaultInputDistance = 10;

    public static ConfirmArrivalRequest Create(params string[] pickupTaskIds)
        => new(pickupTaskIds, DefaultReasonId, DefaultInputDistance);
}
