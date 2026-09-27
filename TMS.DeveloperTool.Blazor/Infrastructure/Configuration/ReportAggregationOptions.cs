using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class ReportAggregationOptions
{
    public const string SectionName = "ReportAggregation";

    /// <summary>Vietnam wall-clock time the daily job runs (aggregates up to yesterday).</summary>
    public TimeOnly RunAt { get; init; } = new(0, 15);

    /// <summary>
    /// Days after a day ends during which it keeps being re-aggregated on each run, to pick up
    /// logs synced late and manifests / handovers finished on the following days.
    /// </summary>
    [Range(0, 30)]
    public int SettleDays { get; init; } = 3;

    /// <summary>First day to aggregate. Unset: the earliest day found in the pro.*_logs tables.</summary>
    public DateOnly? BackfillFromDate { get; init; }
}
