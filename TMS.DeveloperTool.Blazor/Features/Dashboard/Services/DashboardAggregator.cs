using System.Globalization;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Services;

/// <summary>
/// Pure, in-memory aggregations over a <see cref="DashboardSnapshot"/>. Every date/hour is taken in
/// Vietnam time so a 23:30 VN event never lands on the previous UTC day.
/// </summary>
public static class DashboardAggregator
{
    public const string UnknownActor = "(không rõ)";

    /// <summary>Monday-first, matching <see cref="BuildWeekdayHourHeatmap"/> row order.</summary>
    public static readonly string[] WeekdayLabels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

    public static readonly string[] HourLabels = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToArray();

    public static DateTimeOffset ToVietnam(DateTimeOffset value) => VietnamTimeHelper.ToVietnamTime(value);

    public static DateOnly ToVietnamDate(DateTimeOffset value) => DateOnly.FromDateTime(ToVietnam(value).DateTime);

    public static DateOnly BucketStart(DateOnly date, string granularity) => granularity switch
    {
        DashboardGranularity.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
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

    /// <summary>Per-bucket event counts for each event type present in <paramref name="eventTypes"/>.</summary>
    public static TrendResult BuildTrend(
        IEnumerable<DashboardEvent> events,
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        IEnumerable<string> eventTypes,
        Func<DashboardEvent, double>? valueSelector = null)
    {
        valueSelector ??= _ => 1;
        List<DateOnly> buckets = BuildBuckets(startDate, endDate, granularity);
        Dictionary<DateOnly, int> bucketIndex = buckets.Select((bucket, index) => (bucket, index)).ToDictionary(x => x.bucket, x => x.index);
        Dictionary<string, double[]> series = eventTypes.Distinct().ToDictionary(type => type, _ => new double[buckets.Count]);

        foreach (DashboardEvent item in events)
        {
            if (!series.TryGetValue(item.EventType, out double[]? values))
            {
                continue;
            }

            if (bucketIndex.TryGetValue(BucketStart(ToVietnamDate(item.Timestamp), granularity), out int index))
            {
                values[index] += valueSelector(item);
            }
        }

        return new TrendResult(buckets.Select(b => FormatBucket(b, granularity)).ToArray(), series);
    }

    /// <summary>Events per hour of day (0–23). With <paramref name="asPercent"/> each value is the share of the total.</summary>
    public static double[] BuildHourProfile(IEnumerable<DashboardEvent> events, bool asPercent)
    {
        double[] hours = new double[24];
        foreach (DashboardEvent item in events)
        {
            hours[ToVietnam(item.Timestamp).Hour]++;
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
    public static double[][] BuildWeekdayHourHeatmap(IEnumerable<DashboardEvent> events)
    {
        double[][] grid = Enumerable.Range(0, 7).Select(_ => new double[24]).ToArray();
        foreach (DashboardEvent item in events)
        {
            DateTimeOffset local = ToVietnam(item.Timestamp);
            grid[((int)local.DayOfWeek + 6) % 7][local.Hour]++;
        }

        return grid;
    }

    public static TimeOfDayStats BuildTimeOfDayStats(string eventType, IEnumerable<DashboardEvent> events)
    {
        List<TimeSpan> times = events.Select(x => ToVietnam(x.Timestamp).TimeOfDay).Order().ToList();
        if (times.Count == 0)
        {
            return new TimeOfDayStats(eventType, 0, null, null, null, null);
        }

        int peakHour = times.GroupBy(t => t.Hours).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        return new TimeOfDayStats(eventType, times.Count, Percentile(times, 0.1), Percentile(times, 0.5), Percentile(times, 0.9), peakHour);
    }

    public static List<ActorStat> BuildActorStats(IEnumerable<DashboardEvent> events)
        => events
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Actor) ? UnknownActor : x.Actor.Trim())
            .Select(group =>
            {
                List<DateTimeOffset> timestamps = group.Select(x => x.Timestamp).ToList();
                int peakHour = timestamps.GroupBy(t => ToVietnam(t).Hour).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
                return new ActorStat(
                    group.Key,
                    group.Count(),
                    group.Sum(x => x.Quantity),
                    group.Sum(x => x.Amount),
                    timestamps.Select(ToVietnamDate).Distinct().Count(),
                    timestamps.Min(),
                    timestamps.Max(),
                    peakHour);
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Actor, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static DurationStats BuildDurationStats(IEnumerable<TimeSpan> durations)
    {
        List<TimeSpan> sorted = durations.Where(d => d >= TimeSpan.Zero).Order().ToList();
        if (sorted.Count == 0)
        {
            return new DurationStats(0, null, null, null, null);
        }

        TimeSpan average = TimeSpan.FromTicks((long)sorted.Average(d => d.Ticks));
        return new DurationStats(sorted.Count, average, Percentile(sorted, 0.5), Percentile(sorted, 0.9), sorted[^1]);
    }

    /// <summary>
    /// Counts values into buckets split at <paramref name="edges"/> (ascending, exclusive upper bound):
    /// edges [15, 30] give "&lt; 15", "15–30" and "≥ 30".
    /// </summary>
    public static BucketedCounts BuildBuckets(IEnumerable<double> values, double[] edges, Func<double, string> formatEdge)
    {
        double[] counts = new double[edges.Length + 1];
        foreach (double value in values)
        {
            int index = Array.FindIndex(edges, edge => value < edge);
            counts[index < 0 ? edges.Length : index]++;
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

    /// <summary>
    /// Joins created manifests with their first check-in and last task completion by manifest code.
    /// Only arrivals/completions at or after the manifest's creation count.
    /// </summary>
    public static List<ManifestLifecycle> BuildManifestLifecycles(
        IEnumerable<ManifestEvent> commits,
        IEnumerable<ManifestEvent> arrivals,
        IEnumerable<ManifestEvent> completes)
    {
        ILookup<string, DateTimeOffset> arrivalsByCode = arrivals.Where(x => x.ManifestCode != string.Empty).ToLookup(x => x.ManifestCode, x => x.Timestamp);
        ILookup<string, DateTimeOffset> completesByCode = completes.Where(x => x.ManifestCode != string.Empty).ToLookup(x => x.ManifestCode, x => x.Timestamp);

        return commits
            .Where(x => x.ManifestCode != string.Empty)
            .GroupBy(x => x.ManifestCode)
            .Select(group =>
            {
                DateTimeOffset committedAt = group.Min(x => x.Timestamp);
                List<DateTimeOffset> arrivalTimes = arrivalsByCode[group.Key].Where(t => t >= committedAt).ToList();
                List<DateTimeOffset> completeTimes = completesByCode[group.Key].Where(t => t >= committedAt).ToList();
                return new ManifestLifecycle(
                    group.Key,
                    committedAt,
                    arrivalTimes.Count == 0 ? null : arrivalTimes.Min(),
                    completeTimes.Count == 0 ? null : completeTimes.Max());
            })
            .ToList();
    }

    public static string FormatDuration(TimeSpan? value)
    {
        if (value is not { } duration)
        {
            return "—";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.Seconds}s";
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

    private static DateOnly NextBucket(DateOnly bucket, string granularity) => granularity switch
    {
        DashboardGranularity.Week => bucket.AddDays(7),
        DashboardGranularity.Month => bucket.AddMonths(1),
        _ => bucket.AddDays(1)
    };

    /// <summary>Nearest-rank percentile over an already sorted list.</summary>
    private static TimeSpan Percentile(List<TimeSpan> sorted, double percentile)
    {
        int rank = (int)Math.Ceiling(percentile * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }
}
