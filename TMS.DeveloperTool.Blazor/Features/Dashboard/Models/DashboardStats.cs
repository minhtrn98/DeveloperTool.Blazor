namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

/// <summary>Summary of a histogram in its own unit; percentiles are interpolated inside buckets.</summary>
public sealed record HistogramStats(int Count, double? Average, double? Median, double? P90, double? Max);

public sealed record DurationStats(int Count, TimeSpan? Average, TimeSpan? Median, TimeSpan? P90, TimeSpan? Max)
{
    public static readonly DurationStats Empty = new(0, null, null, null, null);

    public static DurationStats FromSeconds(HistogramStats stats) => new(
        stats.Count,
        ToTimeSpan(stats.Average),
        ToTimeSpan(stats.Median),
        ToTimeSpan(stats.P90),
        ToTimeSpan(stats.Max));

    private static TimeSpan? ToTimeSpan(double? seconds) => seconds is { } value ? TimeSpan.FromSeconds(value) : null;
}

/// <summary>Time-of-day spread (Vietnam time): 80% of events fall between <see cref="P10"/> and <see cref="P90"/>.</summary>
public sealed record TimeOfDayStats(string EventType, int Count, TimeSpan? P10, TimeSpan? Median, TimeSpan? P90, int? PeakHour);

public sealed record ActorStat(
    string Actor,
    int Count,
    long Quantity,
    decimal Amount,
    int ActiveDays,
    DateTimeOffset First,
    DateTimeOffset Last,
    int PeakHour)
{
    public double AveragePerActiveDay => ActiveDays == 0 ? 0 : (double)Count / ActiveDays;
}

public sealed record TrendResult(string[] Labels, IReadOnlyDictionary<string, double[]> Series);

public sealed record BucketedCounts(string[] Labels, double[] Counts);
