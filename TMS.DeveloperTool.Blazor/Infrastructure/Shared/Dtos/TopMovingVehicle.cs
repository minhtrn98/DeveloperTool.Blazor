namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed class TopMovingVehicle
{
    public string ActualPlate { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public DateTime LastTime { get; set; }
    public double MaxSpeed { get; set; }
    public double OdoDiff { get; set; }
    public long GpsPoints { get; set; }
    public double MovingScore { get; set; }
}
