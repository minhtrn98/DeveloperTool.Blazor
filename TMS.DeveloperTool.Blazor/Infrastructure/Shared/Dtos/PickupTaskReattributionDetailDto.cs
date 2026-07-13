using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskReattributionDetailDto
{
    public string Tag { get; set; } = string.Empty;
    public string PickupTaskId { get; set; } = string.Empty;
    public PickupTaskStatusId Status { get; set; }
    public bool IsReattributed { get; set; }
    public decimal? CarryShareRatio { get; set; }
}
