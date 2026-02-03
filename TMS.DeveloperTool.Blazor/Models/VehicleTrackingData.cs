namespace TMS.DeveloperTool.Blazor.Models;

public sealed class VehicleTrackingData
{
    public string ActualPlate { get; set; } = string.Empty;
    public double? LastOdoMile { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? TraceUrl { get; set; }
    public string? TraceAddress { get; set; }
    public int Heading { get; set; }
    public int Speed { get; set; }
}