using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

public sealed class GetOrderInManifestResponse
{
    public required string OrderId { get; init; }
    public required OrderType OrderType { get; init; }
    public required OrderStatus OrderStatus { get; init; }
    public required string? ReceiverId { get; init; }
    public required string? ReceiverName { get; init; }
    public required string? ReceiverAddress { get; init; }
    public required decimal? TotalWeight { get; init; }
    public required int TotalItems { get; init; }
    public required decimal? TotalCodAmount { get; init; }
}
