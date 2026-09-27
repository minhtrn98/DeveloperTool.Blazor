using TMS.DeveloperTool.Blazor.Features.Report.Models;

namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

public sealed record LabelCount(string Label, int Count);

/// <summary>
/// Report rows for the range: <see cref="ReportDates"/> come from the report schema,
/// <see cref="LiveDates"/> (today, or days not aggregated yet) are aggregated live with the same
/// SQL. Remaining days of the range are before the first synced log or in the future.
/// </summary>
public sealed record DashboardSnapshot(
    DateOnly StartDate,
    DateOnly EndDate,
    ReportDataSet Data,
    IReadOnlyList<DateOnly> ReportDates,
    IReadOnlyList<DateOnly> LiveDates,
    int StaleHandoverCount)
{
    public int NoDataDayCount => EndDate.DayNumber - StartDate.DayNumber + 1 - ReportDates.Count - LiveDates.Count;
}
