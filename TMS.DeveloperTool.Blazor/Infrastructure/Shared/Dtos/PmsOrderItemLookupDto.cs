using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PmsOrderItemLookupDto
{
    public string OrderItemId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public decimal? Weight { get; init; }
    public decimal? CalWeight { get; init; }
    public WarehouseStatus? WarehouseStatus { get; init; }
    public string? DestinationPostOfficeCode { get; init; }
    public string? DestinationPostOfficeName { get; init; }
    public string? CurrentPostOfficeId { get; init; }
    public string? CurrentPostOfficeName { get; init; }
}
