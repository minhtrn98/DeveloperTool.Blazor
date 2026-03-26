namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

public sealed record TransferPickupTaskRequest(
    string PickupTaskId,
    string Reason,
    string? TargetDriverId);