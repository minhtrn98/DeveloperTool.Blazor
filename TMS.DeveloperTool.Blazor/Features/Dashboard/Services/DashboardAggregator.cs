using System.Globalization;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Features.Report.Models;
using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Services;

/// <summary>
/// Pure, in-memory roll-ups of report rows (<see cref="ReportDataSet"/>) over a date range. Dates
/// and hours in the rows are already Vietnam time.
/// </summary>
public static class DashboardAggregator
{
    public const string UnknownActor = "(không rõ)";

    /// <summary>Monday-first, matching <see cref="BuildWeekdayHourHeatmap"/> row order.</summary>
    public static readonly string[] WeekdayLabels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

    public static readonly string[] HourLabels = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToArray();

    private const int TimeBucketMinutes = 10;

    public static DateOnly ToVietnamDate(DateTimeOffset value) => DateOnly.FromDateTime(VietnamTimeHelper.ToVietnamTime(value).DateTime);

    public static DateOnly BucketStart(DateOnly date, string granularity) => granularity switch
    {
        DashboardGranularity.Week => date.AddDays(-WeekdayIndex(date)),
        DashboardGranularity.Month => new DateOnly(date.Year, date.Month, 1),
        _ => date
    };

    public static List<DateOnly> BuildBuckets(DateOnly startDate, DateOnly endDate, string granularity)
    {
        List<DateOnly> buckets = [];
        for (DateOnly bucket = BucketStart(startDate, granularity); bucket <= endDate; bucket = NextBucket(bucket, granularity))
        {
            buckets.Add(bucket);
        }

        return buckets;
    }

    public static string FormatBucket(DateOnly bucket, string granularity) => granularity switch
    {
        DashboardGranularity.Week => bucket.ToString("'W' dd/MM", CultureInfo.InvariantCulture),
        DashboardGranularity.Month => bucket.ToString("MM/yyyy", CultureInfo.InvariantCulture),
        _ => bucket.ToString("dd/MM", CultureInfo.InvariantCulture)
    };

    /// <summary>Per-bucket event counts (or <paramref name="valueSelector"/> sums) for each type in <paramref name="eventTypes"/>.</summary>
    public static TrendResult BuildTrend(
        IEnumerable<EventStatRow> rows,
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        IEnumerable<string> eventTypes,
        Func<EventStatRow, double>? valueSelector = null)
    {
        valueSelector ??= row => row.Count;
        List<DateOnly> buckets = BuildBuckets(startDate, endDate, granularity);
        Dictionary<DateOnly, int> bucketIndex = buckets.Select((bucket, index) => (bucket, index)).ToDictionary(x => x.bucket, x => x.index);
        Dictionary<string, double[]> series = eventTypes.Distinct().ToDictionary(type => type, _ => new double[buckets.Count]);

        foreach (EventStatRow row in rows)
        {
            if (series.TryGetValue(row.EventType, out double[]? values)
                && bucketIndex.TryGetValue(BucketStart(row.Date, granularity), out int index))
            {
                values[index] += valueSelector(row);
            }
        }

        return new TrendResult(buckets.Select(b => FormatBucket(b, granularity)).ToArray(), series);
    }

    /// <summary>Events per hour of day (0–23). With <paramref name="asPercent"/> each value is the share of the total.</summary>
    public static double[] BuildHourProfile(IEnumerable<TimeBucketRow> rows, bool asPercent)
    {
        double[] hours = new double[24];
        foreach (TimeBucketRow row in rows)
        {
            hours[row.MinuteOfDay / 60] += row.Count;
        }

        double total = hours.Sum();
        if (asPercent && total > 0)
        {
            for (int i = 0; i < hours.Length; i++)
            {
                hours[i] = Math.Round(hours[i] * 100 / total, 1);
            }
        }

        return hours;
    }

    /// <summary>7 rows (Monday → Sunday) × 24 hour columns of event counts.</summary>
    public static double[][] BuildWeekdayHourHeatmap(IEnumerable<TimeBucketRow> rows)
    {
        double[][] grid = Enumerable.Range(0, 7).Select(_ => new double[24]).ToArray();
        foreach (TimeBucketRow row in rows)
        {
            grid[WeekdayIndex(row.Date)][row.MinuteOfDay / 60] += row.Count;
        }

        return grid;
    }

    /// <summary>Percentiles interpolated inside the 10-minute buckets.</summary>
    public static TimeOfDayStats BuildTimeOfDayStats(string eventType, IEnumerable<TimeBucketRow> rows)
    {
        List<(double Lower, double Upper, double Count)> buckets = rows
            .GroupBy(x => x.MinuteOfDay)
            .OrderBy(g => g.Key)
            .Select(g => ((double)g.Key, (double)g.Key + TimeBucketMinutes, (double)g.Sum(x => x.Count)))
            .ToList();
        int count = (int)buckets.Sum(x => x.Count);
        if (count == 0)
        {
            return new TimeOfDayStats(eventType, 0, null, null, null, null);
        }

        int peakHour = buckets.GroupBy(x => (int)x.Lower / 60).OrderByDescending(g => g.Sum(x => x.Count)).ThenBy(g => g.Key).First().Key;
        return new TimeOfDayStats(
            eventType,
            count,
            TimeSpan.FromMinutes(InterpolatedPercentile(buckets, 0.1)),
            TimeSpan.FromMinutes(InterpolatedPercentile(buckets, 0.5)),
            TimeSpan.FromMinutes(InterpolatedPercentile(buckets, 0.9)),
            peakHour);
    }

    public static List<ActorStat> BuildActorStats(IEnumerable<ActorHourRow> rows)
        => rows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Actor) ? UnknownActor : x.Actor)
            .Select(group => new ActorStat(
                group.Key,
                group.Sum(x => x.Count),
                group.Sum(x => x.Quantity),
                group.Sum(x => x.Amount),
                group.Select(x => x.Date).Distinct().Count(),
                ToUtc(group.Min(x => x.FirstAtUtc)),
                ToUtc(group.Max(x => x.LastAtUtc)),
                group.GroupBy(x => x.Hour).OrderByDescending(h => h.Sum(x => x.Count)).ThenBy(h => h.Key).First().Key))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Actor, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static int CountDistinctActors(IEnumerable<ActorHourRow> rows)
        => rows.Where(x => !string.IsNullOrWhiteSpace(x.Actor)).Select(x => x.Actor).Distinct().Count();

    public static List<LabelCount> BuildLabelCounts(IEnumerable<LabelCountRow> rows, string metric)
        => rows
            .Where(x => x.Metric == metric)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Label) ? UnknownActor : x.Label)
            .Select(g => new LabelCount(g.Key, g.Sum(x => x.Count)))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Count / average / max are exact; median and P90 are interpolated inside the fine
    /// histogram buckets defined by <paramref name="edges"/> (the open top bucket ends at its max).
    /// </summary>
    public static HistogramStats BuildHistogramStats(IEnumerable<HistogramRow> rows, string metric, double[] edges)
    {
        List<(int Index, double Lower, int Count, double Sum, double Max)> buckets = MergeBuckets(rows, metric);
        int count = buckets.Sum(x => x.Count);
        if (count == 0)
        {
            return new HistogramStats(0, null, null, null, null);
        }

        List<(double Lower, double Upper, double Count)> ranges = buckets
            .Select(x => (x.Lower, Math.Min(x.Index < edges.Length ? edges[x.Index] : x.Max, x.Max), (double)x.Count))
            .ToList();

        return new HistogramStats(
            count,
            buckets.Sum(x => x.Sum) / count,
            InterpolatedPercentile(ranges, 0.5),
            InterpolatedPercentile(ranges, 0.9),
            buckets.Max(x => x.Max));
    }

    /// <summary>
    /// Re-buckets a fine histogram into coarser <paramref name="edges"/> (same unit, ascending,
    /// exclusive upper bound). Exact as long as every coarse edge is also a fine edge.
    /// </summary>
    public static BucketedCounts Rebucket(IEnumerable<HistogramRow> rows, string metric, double[] edges, Func<double, string> formatEdge)
    {
        double[] counts = new double[edges.Length + 1];
        foreach ((_, double lower, int count, _, _) in MergeBuckets(rows, metric))
        {
            counts[edges.Count(edge => edge <= lower)] += count;
        }

        string[] labels = new string[edges.Length + 1];
        labels[0] = $"< {formatEdge(edges[0])}";
        for (int i = 1; i < edges.Length; i++)
        {
            labels[i] = $"{formatEdge(edges[i - 1])}–{formatEdge(edges[i])}";
        }

        labels[^1] = $"≥ {formatEdge(edges[^1])}";
        return new BucketedCounts(labels, counts);
    }

    /// <summary>Samples with a value ≥ <paramref name="threshold"/> (must be a fine edge).</summary>
    public static int CountAtLeast(IEnumerable<HistogramRow> rows, string metric, double threshold)
        => MergeBuckets(rows, metric).Where(x => x.Lower >= threshold).Sum(x => x.Count);

    public static string FormatDuration(TimeSpan? value)
    {
        if (value is not { } duration)
        {
            return "—";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{(int)duration.TotalSeconds}s";
        }

        if (duration.TotalHours < 1)
        {
            return $"{(int)duration.TotalMinutes}p";
        }

        return duration.TotalDays < 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes:00}p"
            : $"{(int)duration.TotalDays}n {duration.Hours}h";
    }

    public static string FormatTimeOfDay(TimeSpan? value) => value is { } time ? time.ToString(@"hh\:mm") : "—";

    public static string FormatPercent(double numerator, double denominator)
        => denominator == 0 ? "—" : (numerator / denominator).ToString("P1", CultureInfo.InvariantCulture);

    /// <summary>Continuous percentile over ascending, non-overlapping buckets, linear inside a bucket.</summary>
    internal static double InterpolatedPercentile(IReadOnlyList<(double Lower, double Upper, double Count)> buckets, double percentile)
    {
        double target = percentile * buckets.Sum(x => x.Count);
        double cumulative = 0;
        foreach ((double lower, double upper, double count) in buckets)
        {
            if (count > 0 && cumulative + count >= target)
            {
                return lower + (upper - lower) * ((target - cumulative) / count);
            }

            cumulative += count;
        }

        return buckets[^1].Upper;
    }

    private static List<(int Index, double Lower, int Count, double Sum, double Max)> MergeBuckets(IEnumerable<HistogramRow> rows, string metric)
        => rows
            .Where(x => x.Metric == metric)
            .GroupBy(x => x.BucketIndex)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, g.First().BucketLower, g.Sum(x => x.Count), g.Sum(x => x.ValueSum), g.Max(x => x.ValueMax)))
            .ToList();

    private static int WeekdayIndex(DateOnly date) => ((int)date.DayOfWeek + 6) % 7;

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateOnly NextBucket(DateOnly bucket, string granularity) => granularity switch
    {
        DashboardGranularity.Week => bucket.AddDays(7),
        DashboardGranularity.Month => bucket.AddMonths(1),
        _ => bucket.AddDays(1)
    };
}
