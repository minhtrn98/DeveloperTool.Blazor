using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.Report.Services;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

/// <summary>
/// Once a day at <see cref="RouteStopTraceLogCleanupOptions.RunAt"/> (Vietnam time) deletes
/// <c>pro.route_stop_trace_logs</c> rows older than the last
/// <see cref="RouteStopTraceLogCleanupOptions.RetentionDays"/> Vietnam days. Also runs once right
/// at startup so a night missed while the app was down is caught up.
/// </summary>
public sealed class RouteStopTraceLogCleanupJob(
    IDbContextFactory<ProApplicationDbContext> dbContextFactory,
    RouteStopTraceLogCleanupOptions options,
    ILogger<RouteStopTraceLogCleanupJob> logger) : BackgroundService
{
    // Deleted in batches so the table is never locked long enough to stall SignozProSyncJob inserts.
    private const int BatchSize = 10_000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Same filter as the other jobs: only a host shutdown may end the loop.
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Route stop trace log cleanup failed.");
            }

            DateTimeOffset nextRun = DailyReportSchedule.NextRunUtc(DateTimeOffset.UtcNow, options.RunAt);
            logger.LogInformation("Next route stop trace log cleanup at {NextRun} (UTC).", nextRun);
            await Task.Delay(nextRun - DateTimeOffset.UtcNow, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        DateOnly today = DailyReportSchedule.ToVietnamDate(DateTimeOffset.UtcNow);
        DateTimeOffset cutoffUtc = DailyReportSchedule.DayBoundsUtc(today.AddDays(-options.RetentionDays)).Start;

        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        int total = 0;
        int deleted;
        do
        {
            deleted = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM pro.route_stop_trace_logs
                WHERE ctid IN (
                    SELECT ctid FROM pro.route_stop_trace_logs
                    WHERE log_timestamp < {cutoffUtc}
                    LIMIT {BatchSize});
                """, cancellationToken);
            total += deleted;
        }
        while (deleted == BatchSize);

        logger.LogInformation(
            "Route stop trace log cleanup: deleted {Deleted} rows with log_timestamp before {Cutoff} (UTC).",
            total, cutoffUtc);
    }
}
