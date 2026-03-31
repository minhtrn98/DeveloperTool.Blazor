namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

public sealed record DriverCancelPickupTaskRequest(
    string PickupTaskId,
    string Reason,
    string[] OrderItemIds);
