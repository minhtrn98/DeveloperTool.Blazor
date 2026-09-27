using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

namespace TMS.DeveloperTool.Blazor.Features.Report.Services;

public static class AggregationRunStatus
{
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}

/// <summary>Latest state of one day in <c>report.aggregation_runs</c>.</summary>
public sealed record AggregationRunInfo(DateOnly ReportDate, int AggregationVersion, string Status, DateTimeOffset? FinishedAt);

/// <summary>Pure scheduling rules for <see cref="DailyReportJob"/> (Vietnam calendar days).</summary>
public static class DailyReportSchedule
{
    /// <summary>UTC bounds [start, end) of a Vietnam calendar day.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) DayBoundsUtc(DateOnly date)
        => (VietnamTimeHelper.FromVietnamLocal(date.ToDateTime(TimeOnly.MinValue)).ToUniversalTime(),
            VietnamTimeHelper.FromVietnamLocal(date.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToUniversalTime());

    public static DateOnly ToVietnamDate(DateTimeOffset value)
        => DateOnly.FromDateTime(VietnamTimeHelper.ToVietnamTime(value).DateTime);

    /// <summary>The next moment (UTC, strictly after <paramref name="nowUtc"/>) the Vietnam wall clock shows <paramref name="runAt"/>.</summary>
    public static DateTimeOffset NextRunUtc(DateTimeOffset nowUtc, TimeOnly runAt)
    {
        DateOnly today = ToVietnamDate(nowUtc);
        DateTimeOffset candidate = VietnamTimeHelper.FromVietnamLocal(today.ToDateTime(runAt)).ToUniversalTime();
        return candidate > nowUtc ? candidate : VietnamTimeHelper.FromVietnamLocal(today.AddDays(1).ToDateTime(runAt)).ToUniversalTime();
    }

    /// <summary>
    /// Days in [<paramref name="from"/>, <paramref name="to"/>] that need (re)aggregating: never run,
    /// failed, built by an older aggregation version, or last built before the day settled —
    /// i.e. before <paramref name="settleWindow"/> had passed since the day ended, while late logs
    /// and next-day check-ins/receipts could still change its numbers.
    /// </summary>
    public static List<DateOnly> DatesToAggregate(
        DateOnly from,
        DateOnly to,
        IReadOnlyDictionary<DateOnly, AggregationRunInfo> runs,
        int currentVersion,
        TimeSpan settleWindow)
    {
        List<DateOnly> dates = [];
        for (DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            if (!runs.TryGetValue(date, out AggregationRunInfo? run)
                || run.Status != AggregationRunStatus.Succeeded
                || run.AggregationVersion < currentVersion
                || run.FinishedAt is null
                || run.FinishedAt.Value < DayBoundsUtc(date).End + settleWindow)
            {
                dates.Add(date);
            }
        }

        return dates;
    }
}
