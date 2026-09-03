namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record HandoverItemDto(
    string OrderId,
    string OrderItemId,
    string? ExtraService,
    decimal? Weight,
    short? OrderType,
    string? RootMailTripExternalId);
