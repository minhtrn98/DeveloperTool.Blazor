namespace TMS.DeveloperTool.Blazor.Features.Report.Models;

// One record per report.daily_* table, at that table's grain. Init-only properties (not positional
// records) so Dapper can map them; timestamps are read as UTC DateTime.

public sealed record EventStatRow
{
    public DateOnly Date { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int Count { get; init; }
    public long Quantity { get; init; }
    public decimal Amount { get; init; }
}

public sealed record TimeBucketRow
{
    public DateOnly Date { get; init; }
    public string EventType { get; init; } = string.Empty;

    /// <summary>Start of the 10-minute bucket, minutes after Vietnam midnight.</summary>
    public int MinuteOfDay { get; init; }

    public int Count { get; init; }
}

public sealed record ActorHourRow
{
    public DateOnly Date { get; init; }
    public string EventType { get; init; } = string.Empty;

    /// <summary>Trimmed actor; empty when the log had none.</summary>
    public string Actor { get; init; } = string.Empty;

    /// <summary>Vietnam hour of day, 0–23.</summary>
    public int Hour { get; init; }

    public int Count { get; init; }
    public long Quantity { get; init; }
    public decimal Amount { get; init; }
    public DateTime FirstAtUtc { get; init; }
    public DateTime LastAtUtc { get; init; }
}

public sealed record LabelCountRow
{
    public DateOnly Date { get; init; }
    public string Metric { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record HistogramRow
{
    public DateOnly Date { get; init; }
    public string Metric { get; init; } = string.Empty;
    public int BucketIndex { get; init; }

    /// <summary>Inclusive lower bound (0 for the bucket below the first edge).</summary>
    public double BucketLower { get; init; }

    public int Count { get; init; }
    public double ValueSum { get; init; }
    public double ValueMax { get; init; }
}

public sealed record HandoverDayRow
{
    public DateOnly Date { get; init; }
    public int Created { get; init; }
    public long Items { get; init; }
    public int Received { get; init; }
    public int Confirmed { get; init; }
    public int Handled { get; init; }
}

public sealed record ManifestDayRow
{
    public DateOnly Date { get; init; }
    public int Created { get; init; }
    public int WithArrival { get; init; }
    public int WithComplete { get; init; }
    public int Idle { get; init; }
}

/// <summary>The contents of every report.daily_* table for a set of days.</summary>
public sealed record ReportDataSet(
    List<EventStatRow> EventStats,
    List<TimeBucketRow> TimeBuckets,
    List<ActorHourRow> ActorHours,
    List<LabelCountRow> LabelCounts,
    List<HistogramRow> Histograms,
    List<HandoverDayRow> HandoverDays,
    List<ManifestDayRow> ManifestDays)
{
    public static ReportDataSet Empty => new([], [], [], [], [], [], []);

    public ReportDataSet Concat(ReportDataSet other) => new(
        [.. EventStats, .. other.EventStats],
        [.. TimeBuckets, .. other.TimeBuckets],
        [.. ActorHours, .. other.ActorHours],
        [.. LabelCounts, .. other.LabelCounts],
        [.. Histograms, .. other.Histograms],
        [.. HandoverDays, .. other.HandoverDays],
        [.. ManifestDays, .. other.ManifestDays]);
}
