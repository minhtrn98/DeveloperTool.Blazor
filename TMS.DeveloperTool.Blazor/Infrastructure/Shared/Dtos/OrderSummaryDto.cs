namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record OrderSummaryDto(
    string OrderId,
    string? CurrentStatusName,
    DateTime CreatedAt,
    string? ExtraService,
    string? ServiceType,
    string? PickupTaskId,
    decimal? Weight
);
