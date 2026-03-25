namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskInfo
{
    public string PickupTaskId { get; set; } = string.Empty;
    public string? AssignedDriverId { get; set; }
    public string? AssignedDriverCode { get; set; }
    public string? AssignedDriverName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledPickupDate { get; set; }
}
