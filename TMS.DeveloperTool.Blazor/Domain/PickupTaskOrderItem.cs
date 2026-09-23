using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class PickupTaskOrderItem
{
    public long Id { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string PickupTaskId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderItemId { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal RealWeight { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Initialized;
    public bool IsProcessCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
