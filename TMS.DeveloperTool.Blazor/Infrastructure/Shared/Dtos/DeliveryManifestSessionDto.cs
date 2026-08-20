using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record DeliveryManifestSessionDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required DeliveryManifestStatus Status { get; init; }
    public string? PlanCode { get; init; }
    public string? PairingCode { get; init; }
    public string? DriverCode { get; init; }
    public string? VehicleLicensePlate { get; init; }
    public required int TotalItems { get; init; }
    public required int TotalCompleted { get; init; }

    public DeliveryItemStatsDto? Stats { get; set; }
}
