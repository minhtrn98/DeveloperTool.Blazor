namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

public sealed record ReschedulePickupTaskRequest(
    string PickupTaskId,
    DateTime RescheduledPickupDt,
    string Reason,
    string[] OrderItemIds);
