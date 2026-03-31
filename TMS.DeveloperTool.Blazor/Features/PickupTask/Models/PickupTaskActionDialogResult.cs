namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Models;

public sealed record PickupTaskActionDialogResult(
    string[] OrderIds,
    string[] OrderItemIds,
    string Reason,
    DateTime? RescheduledPickupDt,
    string? TargetDriverId);