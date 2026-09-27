using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Services;
using TMS.DeveloperTool.Blazor.Features.Report.Models;

namespace TMS.DeveloperTool.Blazor.Tests.Features.Dashboard;

public class DashboardAggregatorTests
{
    private static readonly DateOnly Monday = new(2026, 9, 21);

    private static EventStatRow Stat(DateOnly date, string type, int count, long quantity = 0)
        => new() { Date = date, EventType = type, Count = count, Quantity = quantity };

    private static TimeBucketRow Bucket(DateOnly date, int minuteOfDay, int count, string type = DashboardEventType.Arrival)
        => new() { Date = date, EventType = type, MinuteOfDay = minuteOfDay, Count = count };

    private static HistogramRow Histogram(int index, double lower, int count, double sum, double max, string metric = "m")
        => new() { Date = Monday, Metric = metric, BucketIndex = index, BucketLower = lower, Count = count, ValueSum = sum, ValueMax = max };

    [Fact]
    public void ToVietnamDate_ShouldRollOverToNextDay_WhenUtcEveningIsPastMidnightInVietnam()
    {
        DashboardAggregator.ToVietnamDate(new DateTimeOffset(2026, 9, 1, 18, 30, 0, TimeSpan.Zero)).Should().Be(new DateOnly(2026, 9, 2));
    }

    [Theory]
    [InlineData(DashboardGranularity.Day, "2026-09-24", "2026-09-24")]
    [InlineData(DashboardGranularity.Week, "2026-09-24", "2026-09-21")]
    [InlineData(DashboardGranularity.Week, "2026-09-27", "2026-09-21")]
    [InlineData(DashboardGranularity.Week, "2026-09-21", "2026-09-21")]
    [InlineData(DashboardGranularity.Month, "2026-09-24", "2026-09-01")]
    public void BucketStart_ShouldAlignToGranularity_WithMondayFirstWeeks(string granularity, string date, string expected)
    {
        DashboardAggregator.BucketStart(DateOnly.Parse(date), granularity).Should().Be(DateOnly.Parse(expected));
    }

    [Fact]
    public void BuildBuckets_ShouldCoverWholeRange_ForMonthGranularity()
    {
        List<DateOnly> buckets = DashboardAggregator.BuildBuckets(new DateOnly(2026, 7, 15), new DateOnly(2026, 9, 2), DashboardGranularity.Month);

        buckets.Should().Equal(new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));
    }

    [Fact]
    public void BuildTrend_ShouldSumDailyRowsPerWeekAndSkipUnrequestedTypes()
    {
        List<EventStatRow> rows =
        [
            Stat(Monday, DashboardEventType.ManifestCommit, 3),
            Stat(Monday.AddDays(6), DashboardEventType.ManifestCommit, 2),
            Stat(Monday.AddDays(7), DashboardEventType.ManifestCommit, 4),
            Stat(Monday, DashboardEventType.Failure, 9)
        ];

        TrendResult trend = DashboardAggregator.BuildTrend(
            rows, Monday, Monday.AddDays(7), DashboardGranularity.Week, [DashboardEventType.ManifestCommit]);

        trend.Labels.Should().Equal("W 21/09", "W 28/09");
        trend.Series[DashboardEventType.ManifestCommit].Should().Equal(5, 4);
        trend.Series.Should().NotContainKey(DashboardEventType.Failure);
    }

    [Fact]
    public void BuildTrend_ShouldSumValueSelector_WhenProvided()
    {
        List<EventStatRow> rows = [Stat(Monday, DashboardEventType.TaskComplete, 2, quantity: 7), Stat(Monday.AddDays(1), DashboardEventType.TaskComplete, 1, quantity: 5)];

        TrendResult trend = DashboardAggregator.BuildTrend(
            rows, Monday, Monday.AddDays(1), DashboardGranularity.Day, [DashboardEventType.TaskComplete], x => x.Quantity);

        trend.Series[DashboardEventType.TaskComplete].Should().Equal(7, 5);
    }

    [Fact]
    public void BuildHourProfile_ShouldMergeTenMinuteBucketsIntoPercentPerHour()
    {
        List<TimeBucketRow> rows = [Bucket(Monday, 480, 1), Bucket(Monday, 530, 1), Bucket(Monday.AddDays(1), 600, 2)];

        double[] profile = DashboardAggregator.BuildHourProfile(rows, asPercent: true);

        profile[8].Should().Be(50);
        profile[10].Should().Be(50);
        profile.Sum().Should().Be(100);
    }

    [Fact]
    public void BuildWeekdayHourHeatmap_ShouldPlaceMondayInFirstRowAndSundayInLast()
    {
        double[][] grid = DashboardAggregator.BuildWeekdayHourHeatmap([Bucket(Monday, 540, 3), Bucket(Monday.AddDays(6), 850, 1)]);

        grid.Should().HaveCount(7);
        grid[0][9].Should().Be(3);
        grid[6][14].Should().Be(1);
        grid.Sum(row => row.Sum()).Should().Be(4);
    }

    [Fact]
    public void BuildTimeOfDayStats_ShouldInterpolateInsideTenMinuteBuckets()
    {
        List<TimeBucketRow> rows = [Bucket(Monday, 420, 3), Bucket(Monday.AddDays(1), 420, 2), Bucket(Monday, 480, 5)];

        TimeOfDayStats stats = DashboardAggregator.BuildTimeOfDayStats(DashboardEventType.Arrival, rows);

        stats.Count.Should().Be(10);
        stats.P10.Should().Be(new TimeSpan(7, 2, 0));
        stats.Median.Should().Be(new TimeSpan(7, 10, 0));
        stats.P90.Should().Be(new TimeSpan(8, 8, 0));
        stats.PeakHour.Should().Be(7);
    }

    [Fact]
    public void BuildTimeOfDayStats_ShouldReturnEmptyStats_WhenNoRows()
    {
        DashboardAggregator.BuildTimeOfDayStats(DashboardEventType.Arrival, [])
            .Should().Be(new TimeOfDayStats(DashboardEventType.Arrival, 0, null, null, null, null));
    }

    [Fact]
    public void BuildActorStats_ShouldMergeHoursAndDaysPerActor()
    {
        DateTime t0 = new(2026, 9, 21, 1, 0, 0, DateTimeKind.Utc);
        List<ActorHourRow> rows =
        [
            new() { Date = Monday, EventType = "x", Actor = "bob", Hour = 8, Count = 2, Quantity = 3, FirstAtUtc = t0, LastAtUtc = t0.AddMinutes(30) },
            new() { Date = Monday, EventType = "x", Actor = "bob", Hour = 15, Count = 1, Quantity = 1, FirstAtUtc = t0.AddHours(7), LastAtUtc = t0.AddHours(7) },
            new() { Date = Monday.AddDays(1), EventType = "x", Actor = "bob", Hour = 8, Count = 1, Quantity = 5, FirstAtUtc = t0.AddDays(1), LastAtUtc = t0.AddDays(1) },
            new() { Date = Monday, EventType = "x", Actor = "alice", Hour = 9, Count = 1, FirstAtUtc = t0, LastAtUtc = t0 },
            new() { Date = Monday, EventType = "x", Actor = "", Hour = 9, Count = 2, FirstAtUtc = t0, LastAtUtc = t0 }
        ];

        List<ActorStat> stats = DashboardAggregator.BuildActorStats(rows);

        stats.Select(x => x.Actor).Should().Equal("bob", DashboardAggregator.UnknownActor, "alice");
        ActorStat bob = stats[0];
        bob.Count.Should().Be(4);
        bob.Quantity.Should().Be(9);
        bob.ActiveDays.Should().Be(2);
        bob.AveragePerActiveDay.Should().Be(2);
        bob.PeakHour.Should().Be(8);
        bob.First.Should().Be(new DateTimeOffset(t0));
        bob.Last.Should().Be(new DateTimeOffset(t0.AddDays(1)));
        DashboardAggregator.CountDistinctActors(rows).Should().Be(2);
    }

    [Fact]
    public void BuildHistogramStats_ShouldMergeDaysAndInterpolatePercentiles()
    {
        double[] edges = [60, 120];
        List<HistogramRow> rows =
        [
            Histogram(0, 0, 2, 60, 40),
            Histogram(1, 60, 1, 80, 80),
            Histogram(1, 60, 1, 100, 100),
            Histogram(2, 120, 1, 300, 300),
            Histogram(0, 0, 50, 500, 20, metric: "other")
        ];

        HistogramStats stats = DashboardAggregator.BuildHistogramStats(rows, "m", edges);

        stats.Count.Should().Be(5);
        stats.Average.Should().Be(108);
        stats.Median.Should().Be(70);
        stats.P90.Should().Be(210);
        stats.Max.Should().Be(300);
    }

    [Fact]
    public void BuildHistogramStats_ShouldReturnEmpty_WhenMetricHasNoRows()
    {
        DashboardAggregator.BuildHistogramStats([], "m", [60]).Should().Be(new HistogramStats(0, null, null, null, null));
    }

    [Fact]
    public void Rebucket_ShouldFoldFineBucketsIntoCoarseEdges()
    {
        List<HistogramRow> rows = [Histogram(0, 0, 2, 0, 0), Histogram(1, 60, 3, 0, 0), Histogram(2, 120, 1, 0, 0), Histogram(3, 300, 4, 0, 0)];

        BucketedCounts buckets = DashboardAggregator.Rebucket(rows, "m", [120, 300], x => $"{x}s");

        buckets.Labels.Should().Equal("< 120s", "120s–300s", "≥ 300s");
        buckets.Counts.Should().Equal(5, 1, 4);
        DashboardAggregator.CountAtLeast(rows, "m", 120).Should().Be(5);
    }

    [Fact]
    public void BuildLabelCounts_ShouldSumAcrossDaysForOneMetric()
    {
        List<LabelCountRow> rows =
        [
            new() { Date = Monday, Metric = "failure_type", Label = "A", Count = 2 },
            new() { Date = Monday.AddDays(1), Metric = "failure_type", Label = "A", Count = 3 },
            new() { Date = Monday, Metric = "failure_type", Label = "", Count = 1 },
            new() { Date = Monday, Metric = "transfer_source", Label = "Driver", Count = 9 }
        ];

        DashboardAggregator.BuildLabelCounts(rows, "failure_type")
            .Should().Equal(new LabelCount("A", 5), new LabelCount(DashboardAggregator.UnknownActor, 1));
    }

    [Theory]
    [InlineData(null, "—")]
    [InlineData(45d, "45s")]
    [InlineData(1500d, "25p")]
    [InlineData(5400d, "1h 30p")]
    [InlineData(97200d, "1n 3h")]
    public void FormatDuration_ShouldUseCompactVietnameseUnits(double? seconds, string expected)
    {
        TimeSpan? duration = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : null;

        DashboardAggregator.FormatDuration(duration).Should().Be(expected);
    }

    [Fact]
    public void RangePresetResolve_ShouldReturnPreviousCalendarMonth_ForLastMonth()
    {
        DashboardRangePreset.Resolve(DashboardRangePreset.LastMonth, new DateOnly(2026, 3, 15))
            .Should().Be((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)));
        DashboardRangePreset.Resolve(DashboardRangePreset.Custom, new DateOnly(2026, 3, 15)).Should().BeNull();
    }
}
