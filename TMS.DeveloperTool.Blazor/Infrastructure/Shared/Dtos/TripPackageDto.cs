namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record TripPackageDto(
    string OrderId,
    string OrderItemId,
    DateTime OrderCreatedAt,
    string PostOfficeId,
    string? StatusId,
    string? PackageCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);
