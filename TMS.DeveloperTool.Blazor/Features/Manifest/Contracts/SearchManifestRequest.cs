using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

public sealed class SearchManifestRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public DeliveryManifestStatus? Status { get; init; }
    public DeliveryManifestType? ManifestType { get; init; }
    public Guid? AssignedDriverId { get; init; }
}
