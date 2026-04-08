namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed class VehicleStatusTrackingDto
{
    public required Guid VehicleId { get; init; }
    public required string LicenseNo { get; init; }
    public required string VehicleDepartmentCode { get; init; }
    public required string Plate { get; init; }
    public required string ActualPlate { get; init; }

    public Guid? DriverId { get; init; }
    public string? DriverCode { get; init; }
    public string? DriverName { get; init; }
}
