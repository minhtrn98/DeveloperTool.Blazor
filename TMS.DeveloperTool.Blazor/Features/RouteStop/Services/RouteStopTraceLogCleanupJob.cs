using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.Report.Services;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

/// <summary>
/// Once a day at <see cref="RouteStopTraceLogCleanupOptions.RunAt"/> (Vietnam time) deletes
/// <c>pro.route_stop_trace_logs</c> rows older than the last
/// <see cref="RouteStopTraceLogCleanupOptions.RetentionDays"/> Vietnam days, and
/// <c>pro.pickup_task_orders</c> / <c>pro.pickup_task_order_items</c> rows older than the last
/// <see cref="RouteStopTraceLogCleanupOptions.PickupTaskOrderRetentionDays"/> Vietnam days. Also runs
/// once right at startup so a night missed while the app was down is caught up.
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
        DateTimeOffset traceLogCutoffUtc = DailyReportSchedule.DayBoundsUtc(today.AddDays(-options.RetentionDays)).Start;
        DateTimeOffset pickupTaskOrderCutoffUtc = DailyReportSchedule.DayBoundsUtc(today.AddDays(-options.PickupTaskOrderRetentionDays)).Start;

        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await DeleteOlderThanAsync(dbContext, "pro.route_stop_trace_logs", "log_timestamp", traceLogCutoffUtc, cancellationToken);
        await DeleteOlderThanAsync(dbContext, "pro.pickup_task_order_items", "created_at", pickupTaskOrderCutoffUtc, cancellationToken);
        await DeleteOlderThanAsync(dbContext, "pro.pickup_task_orders", "created_at", pickupTaskOrderCutoffUtc, cancellationToken);
    }

    // table/column are compile-time constants from RunOnceAsync, never user input.
    private async Task DeleteOlderThanAsync(
        ProApplicationDbContext dbContext,
        string table,
        string timestampColumn,
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken)
    {
        int total = 0;
        int deleted;
        do
        {
#pragma warning disable EF1002 // table/column names are constants, values are parameters.
            deleted = await dbContext.Database.ExecuteSqlRawAsync($$"""
                DELETE FROM {{table}}
                WHERE ctid IN (
                    SELECT ctid FROM {{table}}
                    WHERE {{timestampColumn}} < {0}
                    LIMIT {1});
                """, [cutoffUtc, BatchSize], cancellationToken);
#pragma warning restore EF1002
            total += deleted;
        }
        while (deleted == BatchSize);

        logger.LogInformation(
            "Cleanup {Table}: deleted {Deleted} rows with {Column} before {Cutoff} (UTC).",
            table, total, timestampColumn, cutoffUtc);
    }
}
