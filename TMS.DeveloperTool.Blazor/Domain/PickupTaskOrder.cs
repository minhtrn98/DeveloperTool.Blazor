using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class PickupTaskOrder
{
    public long Id { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string PickupTaskId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ExtraServices { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal RealWeight { get; set; }
    public PickupTaskOrderStatus Status { get; set; } = PickupTaskOrderStatus.New;
    public bool IsProcessCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
