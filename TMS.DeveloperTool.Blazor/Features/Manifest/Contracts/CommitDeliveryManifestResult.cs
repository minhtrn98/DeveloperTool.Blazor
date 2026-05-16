namespace TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

public sealed class CommitDeliveryManifestResult
{
    public Guid? ExpressManifestId { get; init; }
    public string? ExpressManifestCode { get; init; }
    public Guid? CodManifestId { get; init; }
    public string? CodManifestCode { get; init; }
    public List<ManifestItemError> Errors { get; init; } = [];

    public bool HasErrors => Errors.Count > 0;
}

public sealed class ManifestItemError
{
    public required string OrderId { get; init; }
    public required string ItemId { get; init; }
    public required string ErrorCode { get; init; }
    public required string ErrorMessage { get; init; }
}
