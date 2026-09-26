using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.Dashboard;

public class DashboardAggregatorTests
{
    private static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);

    private static DashboardEvent Event(string type, DateTimeOffset timestamp, string actor = "user1", int quantity = 0)
        => new(type, timestamp, actor, quantity);

    [Fact]
    public void ToVietnamDate_ShouldRollOverToNextDay_WhenUtcEveningIsPastMidnightInVietnam()
    {
        DateTimeOffset utcEvening = new(2026, 9, 1, 18, 30, 0, TimeSpan.Zero);

        DashboardAggregator.ToVietnamDate(utcEvening).Should().Be(new DateOnly(2026, 9, 2));
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
    public void BuildTrend_ShouldCountPerBucketAndType_UsingVietnamDates()
    {
        List<DashboardEvent> events =
        [
            Event(DashboardEventType.ManifestCommit, new DateTimeOffset(2026, 9, 1, 8, 0, 0, Vietnam)),
            Event(DashboardEventType.ManifestCommit, new DateTimeOffset(2026, 9, 1, 23, 30, 0, Vietnam)),
            Event(DashboardEventType.ManifestCommit, new DateTimeOffset(2026, 9, 2, 0, 15, 0, Vietnam)),
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 2, 9, 0, 0, Vietnam)),
            Event(DashboardEventType.Failure, new DateTimeOffset(2026, 9, 2, 9, 0, 0, Vietnam))
        ];

        TrendResult trend = DashboardAggregator.BuildTrend(
            events, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3), DashboardGranularity.Day,
            [DashboardEventType.ManifestCommit, DashboardEventType.Arrival]);

        trend.Labels.Should().Equal("01/09", "02/09", "03/09");
        trend.Series[DashboardEventType.ManifestCommit].Should().Equal(2, 1, 0);
        trend.Series[DashboardEventType.Arrival].Should().Equal(0, 1, 0);
        trend.Series.Should().NotContainKey(DashboardEventType.Failure);
    }

    [Fact]
    public void BuildTrend_ShouldSumValueSelector_WhenProvided()
    {
        List<DashboardEvent> events =
        [
            Event(DashboardEventType.TaskComplete, new DateTimeOffset(2026, 9, 1, 8, 0, 0, Vietnam), quantity: 3),
            Event(DashboardEventType.TaskComplete, new DateTimeOffset(2026, 9, 1, 9, 0, 0, Vietnam), quantity: 4)
        ];

        TrendResult trend = DashboardAggregator.BuildTrend(
            events, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), DashboardGranularity.Day,
            [DashboardEventType.TaskComplete], x => x.Quantity);

        trend.Series[DashboardEventType.TaskComplete].Should().Equal(7);
    }

    [Fact]
    public void BuildHourProfile_ShouldReturnPercentShares_InVietnamHours()
    {
        List<DashboardEvent> events =
        [
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 1, 1, 0, 0, TimeSpan.Zero)),
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 1, 1, 30, 0, TimeSpan.Zero)),
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 1, 3, 0, 0, TimeSpan.Zero)),
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 1, 3, 10, 0, TimeSpan.Zero))
        ];

        double[] profile = DashboardAggregator.BuildHourProfile(events, asPercent: true);

        profile[8].Should().Be(50);
        profile[10].Should().Be(50);
        profile.Sum().Should().Be(100);
    }

    [Fact]
    public void BuildWeekdayHourHeatmap_ShouldPlaceMondayInFirstRowAndSundayInLast()
    {
        List<DashboardEvent> events =
        [
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 21, 9, 0, 0, Vietnam)),
            Event(DashboardEventType.Arrival, new DateTimeOffset(2026, 9, 27, 14, 0, 0, Vietnam))
        ];

        double[][] grid = DashboardAggregator.BuildWeekdayHourHeatmap(events);

        grid.Should().HaveCount(7);
        grid[0][9].Should().Be(1);
        grid[6][14].Should().Be(1);
        grid.Sum(row => row.Sum()).Should().Be(2);
    }

    [Fact]
    public void BuildTimeOfDayStats_ShouldReturnPercentilesAndPeakHour()
    {
        List<DashboardEvent> events = Enumerable.Range(0, 10)
            .Select(i => Event(DashboardEventType.ManifestCommit, new DateTimeOffset(2026, 9, 1, 6 + i, 0, 0, Vietnam)))
            .Append(Event(DashboardEventType.ManifestCommit, new DateTimeOffset(2026, 9, 2, 7, 30, 0, Vietnam)))
            .ToList();

        TimeOfDayStats stats = DashboardAggregator.BuildTimeOfDayStats(DashboardEventType.ManifestCommit, events);

        stats.Count.Should().Be(11);
        stats.PeakHour.Should().Be(7);
        stats.P10.Should().Be(TimeSpan.FromHours(7));
        stats.Median.Should().Be(TimeSpan.FromHours(10));
        stats.P90.Should().Be(TimeSpan.FromHours(14));
    }

    [Fact]
    public void BuildTimeOfDayStats_ShouldReturnEmptyStats_WhenNoEvents()
    {
        TimeOfDayStats stats = DashboardAggregator.BuildTimeOfDayStats(DashboardEventType.Arrival, []);

        stats.Should().Be(new TimeOfDayStats(DashboardEventType.Arrival, 0, null, null, null, null));
    }

    [Fact]
    public void BuildActorStats_ShouldGroupByActorAndOrderByCountDescending()
    {
        List<DashboardEvent> events =
        [
            Event(DashboardEventType.Failure, new DateTimeOffset(2026, 9, 1, 8, 0, 0, Vietnam), "alice", 2),
            Event(DashboardEventType.Failure, new DateTimeOffset(2026, 9, 1, 8, 30, 0, Vietnam), "bob", 1),
            Event(DashboardEventType.Failure, new DateTimeOffset(2026, 9, 2, 8, 45, 0, Vietnam), "bob", 5),
            Event(DashboardEventType.Failure, new DateTimeOffset(2026, 9, 3, 15, 0, 0, Vietnam), "bob", 1),
            Event(DashboardEventType.Failure, new DateTimeOffset(2026, 9, 3, 15, 0, 0, Vietnam), " ", 1)
        ];

        List<ActorStat> stats = DashboardAggregator.BuildActorStats(events);

        stats.Select(x => x.Actor).Should().Equal("bob", DashboardAggregator.UnknownActor, "alice");
        ActorStat bob = stats[0];
        bob.Count.Should().Be(3);
        bob.Quantity.Should().Be(7);
        bob.ActiveDays.Should().Be(3);
        bob.AveragePerActiveDay.Should().Be(1);
        bob.PeakHour.Should().Be(8);
        bob.First.Should().Be(new DateTimeOffset(2026, 9, 1, 8, 30, 0, Vietnam));
        bob.Last.Should().Be(new DateTimeOffset(2026, 9, 3, 15, 0, 0, Vietnam));
    }

    [Fact]
    public void BuildDurationStats_ShouldIgnoreNegativeDurations()
    {
        TimeSpan[] durations = [TimeSpan.FromMinutes(-5), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(90)];

        DurationStats stats = DashboardAggregator.BuildDurationStats(durations);

        stats.Count.Should().Be(3);
        stats.Average.Should().Be(TimeSpan.FromMinutes(40));
        stats.Median.Should().Be(TimeSpan.FromMinutes(20));
        stats.P90.Should().Be(TimeSpan.FromMinutes(90));
        stats.Max.Should().Be(TimeSpan.FromMinutes(90));
    }

    [Fact]
    public void BuildBuckets_ShouldSplitValuesAtEdges_WithExclusiveUpperBound()
    {
        BucketedCounts buckets = DashboardAggregator.BuildBuckets([5, 15, 29.9, 30, 1000], [15, 30], x => $"{x}p");

        buckets.Labels.Should().Equal("< 15p", "15p–30p", "≥ 30p");
        buckets.Counts.Should().Equal(1, 2, 2);
    }

    [Fact]
    public void BuildManifestLifecycles_ShouldUseFirstArrivalAndLastCompleteAfterCommit()
    {
        DateTimeOffset committedAt = new(2026, 9, 1, 7, 0, 0, Vietnam);
        ManifestEvent[] commits =
        [
            new("DE1", committedAt),
            new("DE1", committedAt.AddMinutes(5)),
            new("DE2", committedAt),
            new(string.Empty, committedAt)
        ];
        ManifestEvent[] arrivals =
        [
            new("DE1", committedAt.AddMinutes(-10)),
            new("DE1", committedAt.AddHours(2)),
            new("DE1", committedAt.AddHours(1))
        ];
        ManifestEvent[] completes =
        [
            new("DE1", committedAt.AddHours(3)),
            new("DE1", committedAt.AddHours(6))
        ];

        List<ManifestLifecycle> lifecycles = DashboardAggregator.BuildManifestLifecycles(commits, arrivals, completes);

        lifecycles.Should().HaveCount(2);
        lifecycles.Single(x => x.ManifestCode == "DE1").Should().Be(
            new ManifestLifecycle("DE1", committedAt, committedAt.AddHours(1), committedAt.AddHours(6)));
        lifecycles.Single(x => x.ManifestCode == "DE2").Should().Be(
            new ManifestLifecycle("DE2", committedAt, null, null));
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
        (DateOnly Start, DateOnly End)? range = DashboardRangePreset.Resolve(DashboardRangePreset.LastMonth, new DateOnly(2026, 3, 15));

        range.Should().Be((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)));
        DashboardRangePreset.Resolve(DashboardRangePreset.Custom, new DateOnly(2026, 3, 15)).Should().BeNull();
    }
}
