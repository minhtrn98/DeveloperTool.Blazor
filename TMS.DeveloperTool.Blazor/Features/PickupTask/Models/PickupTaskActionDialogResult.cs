namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Models;

public sealed record PickupTaskActionDialogResult(
    string[] OrderItemIds,
    string Reason,
    DateTime? RescheduledPickupDt,
    string? TargetDriverId);