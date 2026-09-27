using Dapper;
using Npgsql;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Features.Report.Models;
using TMS.DeveloperTool.Blazor.Features.Report.Services;

namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Services;

/// <summary>
/// Loads dashboard data for a Vietnam-calendar date range. Past days already aggregated by
/// <see cref="DailyReportJob"/> at the current <see cref="DailyReportAggregator.AggregationVersion"/>
/// are read from <c>report.daily_*</c>; every other day (today, or a day the job hasn't built yet)
/// is aggregated live from pro.*_logs with the same SQL, so both sources report identical numbers.
/// </summary>
public sealed class DashboardProStorageService(
    ConnectionStringsOptions connectionStrings,
    ReportSchemaMigrator migrator,
    DailyReportAggregator aggregator)
{
    /// <summary>A handover still unreceived this long after creation counts as stale.</summary>
    public static readonly TimeSpan StaleHandoverAge = TimeSpan.FromHours(4);

    public async Task<DashboardSnapshot> LoadAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        await migrator.EnsureMigratedAsync().WaitAsync(cancellationToken);

        // Days before the first synced log, or after today, have nothing to read or aggregate.
        DateOnly today = DailyReportSchedule.ToVietnamDate(DateTimeOffset.UtcNow);
        DateOnly firstDay = await aggregator.GetEarliestLogDateAsync(cancellationToken) ?? today;
        List<DateOnly> allDates = [];
        for (DateOnly date = startDate < firstDay ? firstDay : startDate; date <= endDate && date <= today; date = date.AddDays(1))
        {
            allDates.Add(date);
        }

        Dictionary<DateOnly, AggregationRunInfo> runs = allDates.Count == 0
            ? []
            : await aggregator.GetRunsAsync(allDates[0], allDates[^1], cancellationToken);
        List<DateOnly> reportDates = allDates.Where(date => date < today && IsUsable(runs.GetValueOrDefault(date))).ToList();
        List<DateOnly> liveDates = allDates.Except(reportDates).ToList();

        Task<ReportDataSet> reportTask = ReadReportAsync(reportDates, cancellationToken);
        Task<ReportDataSet> liveTask = aggregator.AggregateLiveAsync(liveDates, cancellationToken);
        Task<int> staleTask = CountStaleHandoversAsync(startDate, endDate, cancellationToken);
        await Task.WhenAll(reportTask, liveTask, staleTask);

        return new DashboardSnapshot(startDate, endDate, reportTask.Result.Concat(liveTask.Result), reportDates, liveDates, staleTask.Result);
    }

    private static bool IsUsable(AggregationRunInfo? run)
        => run is { Status: AggregationRunStatus.Succeeded } && run.AggregationVersion == DailyReportAggregator.AggregationVersion;

    private async Task<ReportDataSet> ReadReportAsync(List<DateOnly> dates, CancellationToken cancellationToken)
    {
        if (dates.Count == 0)
        {
            return ReportDataSet.Empty;
        }

        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        await connection.OpenAsync(cancellationToken);
        return await ReportReader.ReadAsync(connection, null, "report", dates, cancellationToken);
    }

    // Depends on "now", so it is always read live rather than pre-aggregated.
    private async Task<int> CountStaleHandoversAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        DateTimeOffset from = DailyReportSchedule.DayBoundsUtc(startDate).Start;
        DateTimeOffset to = DailyReportSchedule.DayBoundsUtc(endDate).End;
        DateTimeOffset staleBefore = DateTimeOffset.UtcNow - StaleHandoverAge;

        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT count(*)::int FROM pro.unloading_handover_logs
            WHERE log_timestamp >= @From AND log_timestamp < @To AND log_timestamp < @StaleBefore
              AND received_at IS NULL AND confirm_at IS NULL
            """,
            new { From = from, To = to, StaleBefore = staleBefore },
            cancellationToken: cancellationToken));
    }
}
