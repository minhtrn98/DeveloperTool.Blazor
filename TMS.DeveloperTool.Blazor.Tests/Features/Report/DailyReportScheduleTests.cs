using TMS.DeveloperTool.Blazor.Features.Report.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.Report;

public class DailyReportScheduleTests
{
    private static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);
    private static readonly TimeOnly RunAt = new(0, 15);
    private static readonly TimeSpan SettleWindow = TimeSpan.FromDays(3);

    [Fact]
    public void DayBoundsUtc_ShouldCoverVietnamMidnightToMidnight()
    {
        (DateTimeOffset start, DateTimeOffset end) = DailyReportSchedule.DayBoundsUtc(new DateOnly(2026, 9, 26));

        start.Should().Be(new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero));
        end.Should().Be(new DateTimeOffset(2026, 9, 26, 17, 0, 0, TimeSpan.Zero));
        start.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void NextRunUtc_ShouldReturnTonight_WhenRunTimeHasPassedToday()
    {
        DateTimeOffset now = new(2026, 9, 26, 10, 0, 0, Vietnam);

        DailyReportSchedule.NextRunUtc(now, RunAt).Should().Be(new DateTimeOffset(2026, 9, 27, 0, 15, 0, Vietnam));
    }

    [Fact]
    public void NextRunUtc_ShouldReturnToday_WhenRunTimeIsStillAhead()
    {
        DateTimeOffset now = new(2026, 9, 27, 0, 5, 0, Vietnam);

        DailyReportSchedule.NextRunUtc(now, RunAt).Should().Be(new DateTimeOffset(2026, 9, 27, 0, 15, 0, Vietnam));
    }

    [Fact]
    public void NextRunUtc_ShouldBeStrictlyAfterNow_WhenCalledExactlyAtRunTime()
    {
        DateTimeOffset now = new(2026, 9, 27, 0, 15, 0, Vietnam);

        DailyReportSchedule.NextRunUtc(now, RunAt).Should().Be(new DateTimeOffset(2026, 9, 28, 0, 15, 0, Vietnam));
    }

    [Fact]
    public void DatesToAggregate_ShouldReturnMissingFailedAndOutdatedDays()
    {
        DateOnly from = new(2026, 9, 1);
        DateOnly to = new(2026, 9, 5);
        DateTimeOffset longAfter = new(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);
        Dictionary<DateOnly, AggregationRunInfo> runs = new()
        {
            [new(2026, 9, 1)] = new(new(2026, 9, 1), 2, AggregationRunStatus.Succeeded, longAfter),
            [new(2026, 9, 2)] = new(new(2026, 9, 2), 2, AggregationRunStatus.Failed, longAfter),
            [new(2026, 9, 3)] = new(new(2026, 9, 3), 1, AggregationRunStatus.Succeeded, longAfter),
            [new(2026, 9, 5)] = new(new(2026, 9, 5), 2, AggregationRunStatus.Succeeded, longAfter)
        };

        List<DateOnly> dates = DailyReportSchedule.DatesToAggregate(from, to, runs, currentVersion: 2, SettleWindow);

        dates.Should().Equal(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 4));
    }

    [Fact]
    public void DatesToAggregate_ShouldRebuildDay_UntilARunFinishesAfterItSettled()
    {
        DateOnly day = new(2026, 9, 20);
        DateTimeOffset dayEnd = DailyReportSchedule.DayBoundsUtc(day).End;

        List<DateOnly> builtNextNight = DailyReportSchedule.DatesToAggregate(day, day,
            new Dictionary<DateOnly, AggregationRunInfo> { [day] = new(day, 1, AggregationRunStatus.Succeeded, dayEnd.AddMinutes(15)) },
            currentVersion: 1, SettleWindow);
        List<DateOnly> builtAfterSettling = DailyReportSchedule.DatesToAggregate(day, day,
            new Dictionary<DateOnly, AggregationRunInfo> { [day] = new(day, 1, AggregationRunStatus.Succeeded, dayEnd + SettleWindow + TimeSpan.FromMinutes(15)) },
            currentVersion: 1, SettleWindow);

        builtNextNight.Should().Equal(day);
        builtAfterSettling.Should().BeEmpty();
    }

    [Fact]
    public void LoadScripts_ShouldFindEmbeddedMigrationsInVersionOrder()
    {
        List<ReportSchemaMigrator.MigrationScript> scripts = ReportSchemaMigrator.LoadScripts();

        scripts.Should().NotBeEmpty();
        scripts[0].Version.Should().Be(1);
        scripts[0].ScriptName.Should().Be("V001__create_report_schema.sql");
        scripts[0].Sql.Should().Contain("CREATE SCHEMA IF NOT EXISTS report");
        scripts.Select(x => x.Version).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
    }
}
