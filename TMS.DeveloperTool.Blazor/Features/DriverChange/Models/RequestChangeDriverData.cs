namespace TMS.DeveloperTool.Blazor.Features.DriverChange.Models;

public sealed record RequestChangeDriverData(
    Guid VehicleId,
    double Odometer,
    Guid ImageId,
    int ActionType,
    Guid DriverRequest,
    bool IsConfirm,
    bool IsCancel,
    Guid CreatedBy
)
{
    public string Key { get; set; } = default!;
    public string? DriverRequestName { get; set; }
    public string? VehicleLicensePlate { get; set; }
}
