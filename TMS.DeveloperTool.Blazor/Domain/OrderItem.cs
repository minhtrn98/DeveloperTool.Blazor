namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class OrderItem
{
    public string OrderItemId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public decimal? Weight { get; init; }
    public decimal? RealWeight { get; init; }
    public decimal? CalWeight { get; init; }
    public decimal? L { get; init; }
    public decimal? H { get; init; }
    public decimal? W { get; init; }
    public string? CurrentPostOfficeName { get; init; }
    public string? CurrentStatusId { get; init; }
    public string? CurrentStatusName { get; init; }
}
