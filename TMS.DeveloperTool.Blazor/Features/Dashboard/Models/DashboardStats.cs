namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

public sealed record DurationStats(int Count, TimeSpan? Average, TimeSpan? Median, TimeSpan? P90, TimeSpan? Max);

/// <summary>Time-of-day spread (Vietnam time): 80% of events fall between <see cref="P10"/> and <see cref="P90"/>.</summary>
public sealed record TimeOfDayStats(string EventType, int Count, TimeSpan? P10, TimeSpan? Median, TimeSpan? P90, int? PeakHour);

public sealed record ActorStat(
    string Actor,
    int Count,
    int Quantity,
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
