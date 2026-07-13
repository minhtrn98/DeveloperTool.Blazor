using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskReattributionDto
{
    public string PickupTaskId { get; set; } = string.Empty;
    public PickupTaskStatusId Status { get; set; }
}
