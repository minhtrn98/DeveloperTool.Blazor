namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record OrderItemJourneyDto
{
    public required string ManifestCode { get; init; }
    public string? DriverCode { get; init; }
    public required string Operation { get; init; }
    public required string Action { get; init; }
    public DateTimeOffset? ActionAt { get; init; }
    public string? VehicleLicensePlate { get; init; }
    public string? AssignmentCode { get; init; }
    public string? PlanCode { get; init; }
    public string? HandoverId { get; init; }
    public double? Lat { get; init; }
    public double? Lng { get; init; }
    public string? StopCode { get; init; }
    public string? StopName { get; init; }
    public int? MisplacedReason { get; init; }
    public string? MisplacedReasonName { get; init; }
}
