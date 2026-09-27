using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class RouteStopTraceLogCleanupOptions
{
    public const string SectionName = "RouteStopTraceLogCleanup";

    /// <summary>Vietnam wall-clock time the daily cleanup runs.</summary>
    public TimeOnly RunAt { get; init; } = new(0, 0);

    /// <summary>
    /// Number of most recent full Vietnam days kept (plus today). Rows with <c>log_timestamp</c>
    /// before the start of (today - RetentionDays) are deleted.
    /// </summary>
    [Range(1, 365)]
    public int RetentionDays { get; init; } = 3;
}
