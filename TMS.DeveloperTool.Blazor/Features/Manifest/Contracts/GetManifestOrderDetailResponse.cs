using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

public sealed class GetManifestOrderDetailResponse
{
    public required string OrderId { get; init; }
    public required OrderType OrderType { get; init; }
    public required OrderStatus OrderStatus { get; init; }
    public required string? ReceiverId { get; init; }
    public required string? ReceiverName { get; init; }
    public required string? ReceiverAddress { get; init; }
    public required string? ReceiverPostOfficeId { get; init; }
    public required string? ReceiverPostOfficeName { get; init; }
    public required decimal? TotalWeight { get; init; }
    public required int TotalItems { get; init; }
    public required decimal? TotalCodAmount { get; init; }
    public string? PicId { get; init; }
    public string? PicCode { get; init; }
    public string? PicName { get; init; }
    public string? PicAvatar { get; init; }
    public GetManifestOrderItemDetail[] Items { get; init; } = [];
}

public sealed record GetManifestOrderItemDetail(string OrderItemId, DeliveryItemStatus Status);
