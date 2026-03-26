namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    public required string DriverDb { get; init; }
    public required string FleetDb { get; init; }
    public required string RouteDb { get; init; }
    public required string PlanningDb { get; init; }
    public required string OrderDb { get; init; }
    public required string TrackingDb { get; init; }
    public required string DeveloperDb { get; init; }
}
