namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskRedistributeItemDto
{
    public string OrderItemId { get; set; } = string.Empty;
    public decimal Weight { get; set; }
}
