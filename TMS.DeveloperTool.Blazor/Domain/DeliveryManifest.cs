using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class DeliveryManifest
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string Code { get; init; } = string.Empty;
    public DeliveryManifestType ManifestType { get; init; }
    public DeliveryMode? DeliveryMode { get; init; }
    public DeliveryManifestStatus Status { get; init; }
    public string? OriginPostOfficeName { get; init; }
    public string? CreatedByName { get; init; }
    public int TotalSessions { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItems { get; init; }
    public int DeliveredItems { get; init; }
    public int FailedItems { get; init; }
    public int LoadedItems { get; init; }
    public decimal? TotalCodAmount { get; init; }
    public decimal? CollectedCodAmount { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
}
