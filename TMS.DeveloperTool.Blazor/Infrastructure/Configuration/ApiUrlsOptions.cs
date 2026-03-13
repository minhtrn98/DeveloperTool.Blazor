namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class ApiUrlsOptions
{
    public const string SectionName = "ApiUrls";

    public required string Fleet { get; init; }
    public required string Driver { get; init; }
    public required string Cost { get; init; }
    public required string Route { get; init; }
    public required string Order { get; init; }
    public required string Planning { get; init; }
    public required string Tracking { get; init; }
    public required string Notification { get; init; }
    public required string Audit { get; init; }
    public required string File { get; init; }
    public required string Report { get; init; }
}