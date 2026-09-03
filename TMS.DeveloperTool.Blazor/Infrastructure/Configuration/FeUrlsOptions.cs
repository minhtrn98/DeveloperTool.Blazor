namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class FeUrlsOptions
{
    public const string SectionName = "FeUrls";

    public required string BaseUrl { get; init; }
}
