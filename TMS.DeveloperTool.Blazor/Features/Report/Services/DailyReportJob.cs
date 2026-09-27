namespace TMS.DeveloperTool.Blazor.Features.Report.Services;

/// <summary>
/// Applies pending report schema migrations, then once a day at
/// <see cref="ReportAggregationOptions.RunAt"/> (Vietnam time) builds <c>report.daily_*</c> for
/// every day up to yesterday that <see cref="DailyReportSchedule.DatesToAggregate"/> says is due —
/// yesterday, days not yet settled, failed days, missing days (backfill) and days built by an
/// older <see cref="DailyReportAggregator.AggregationVersion"/>. Also runs once right at startup
/// so a restart catches up without waiting for the next night.
/// </summary>
public sealed class DailyReportJob(
    ReportSchemaMigrator migrator,
    DailyReportAggregator aggregator,
    ReportAggregationOptions options,
    ILogger<DailyReportJob> logger) : BackgroundService
{
    private static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await MigrateAsync(stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // Same filter as SignozProSyncJob: only a host shutdown may end the loop; timeouts and
            // transient DB errors are logged and retried on the next run.
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Daily report aggregation failed.");
            }

            DateTimeOffset nextRun = DailyReportSchedule.NextRunUtc(DateTimeOffset.UtcNow, options.RunAt);
            logger.LogInformation("Next daily report aggregation at {NextRun} (UTC).", nextRun);
            await Task.Delay(nextRun - DateTimeOffset.UtcNow, stoppingToken);
        }
    }

    private async Task<bool> MigrateAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await migrator.MigrateAsync(stoppingToken);
                return true;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Report schema migration failed; retrying in {Delay}.", MigrationRetryDelay);
                await Task.Delay(MigrationRetryDelay, stoppingToken);
            }
        }

        return false;
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        DateOnly yesterday = DailyReportSchedule.ToVietnamDate(DateTimeOffset.UtcNow).AddDays(-1);
        DateOnly? from = options.BackfillFromDate ?? await aggregator.GetEarliestLogDateAsync(cancellationToken);
        if (from is null || from > yesterday)
        {
            logger.LogInformation("Daily report aggregation: nothing to aggregate yet.");
            return;
        }

        Dictionary<DateOnly, AggregationRunInfo> runs = await aggregator.GetRunsAsync(from.Value, yesterday, cancellationToken);
        List<DateOnly> dates = DailyReportSchedule.DatesToAggregate(
            from.Value, yesterday, runs, DailyReportAggregator.AggregationVersion, TimeSpan.FromDays(options.SettleDays));

        int succeeded = 0;
        foreach (DateOnly date in dates)
        {
            try
            {
                int eventCount = await aggregator.AggregateDayAsync(date, cancellationToken);
                succeeded++;
                logger.LogInformation("Daily report aggregated {ReportDate}: {EventCount} events.", date, eventCount);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Daily report aggregation failed for {ReportDate}.", date);
                await aggregator.MarkFailedAsync(date, ex.Message, cancellationToken);
            }
        }

        logger.LogInformation(
            "Daily report aggregation [{From} - {To}] v{Version}: {Succeeded}/{Due} due days aggregated.",
            from.Value, yesterday, DailyReportAggregator.AggregationVersion, succeeded, dates.Count);
    }
}
