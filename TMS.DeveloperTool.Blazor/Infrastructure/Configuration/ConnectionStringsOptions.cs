namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    // TMS production database connection strings (DriverDb, FleetDb, PlanningDb, OrderDb) are
    // read directly from IConfiguration by ApplicationDbQuery, keyed by database name — they
    // are optional and intentionally not modeled here. Environments without direct DB access
    // (only LogApi) simply omit them; ApplicationDbQuery skips querying when unset.
    public required string DeveloperDb { get; init; }
}
