using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

public sealed class ManifestListItemDto
{
    public Guid DeliveryManifestId { get; init; }
    public string Code { get; init; } = string.Empty;
    public DeliveryManifestStatus Status { get; init; }
    public DeliveryManifestType ManifestType { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? OriginPostOfficeName { get; init; }
    public int TotalSessions { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItems { get; init; }
    public int LoadedItems { get; init; }
    public int DeliveredItems { get; init; }
    public int FailedItems { get; init; }
    public decimal? TotalWeight { get; init; }
    public decimal? TotalCodAmount { get; init; }
}
