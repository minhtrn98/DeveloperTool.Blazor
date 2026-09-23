using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskOrderSync.Services;

/// <summary>
/// Every minute, drains unprocessed rows of <c>pro.pickup_task_trace_logs</c> in batches of
/// <see cref="BatchSize"/>: parses each log's message detail into
/// <c>pro.pickup_task_orders</c> / <c>pro.pickup_task_order_items</c> (insert only, never
/// updated or deleted) and marks the log processed.
/// </summary>
/// <remarks>
/// Each log is saved in its own transaction (rows + processed flag together), so a crash never
/// leaves a log half-applied and a retry never inserts its rows twice. A log that can't be
/// parsed or saved is still marked processed (with its error logged) — otherwise, being the
/// oldest unprocessed row, it would block the queue forever.
/// </remarks>
public sealed class PickupTaskOrderSyncJob(
    IDbContextFactory<ProApplicationDbContext> dbContextFactory,
    ILogger<PickupTaskOrderSyncJob> logger) : BackgroundService
{
    private const int BatchSize = 10;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);
        do
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            // Same reasoning as SignozProSyncJob: only stop when the host itself is shutting down.
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Pickup task order sync failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        int processed;
        do
        {
            processed = await ProcessBatchAsync(cancellationToken);
        }
        while (processed == BatchSize && !cancellationToken.IsCancellationRequested);
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PickupTaskTraceLog> logs = await dbContext.PickupTaskTraceLogs
            .AsNoTracking()
            .Where(log => !log.IsProcessed)
            .OrderBy(log => log.LogTimestamp)
            .ThenBy(log => log.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (PickupTaskTraceLog log in logs)
        {
            await ProcessLogAsync(dbContext, log, cancellationToken);
        }

        return logs.Count;
    }

    private async Task ProcessLogAsync(ProApplicationDbContext dbContext, PickupTaskTraceLog log, CancellationToken cancellationToken)
    {
        PickupTaskOrderParseResult result;
        try
        {
            result = PickupTaskOrderMessageParser.Parse(log);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentOutOfRangeException)
        {
            logger.LogWarning(ex, "Pickup task trace log {LogId} could not be parsed; marking it processed without rows.", log.LogId);
            result = PickupTaskOrderParseResult.Empty;
        }

        try
        {
            await SaveAsync(dbContext, log, result, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Pickup task trace log {LogId} rows could not be saved; marking it processed without rows.", log.LogId);
            await SaveAsync(dbContext, log, PickupTaskOrderParseResult.Empty, cancellationToken);
        }
    }

    private static Task SaveAsync(ProApplicationDbContext dbContext, PickupTaskTraceLog log, PickupTaskOrderParseResult result, CancellationToken cancellationToken)
    {
        // EnableRetryOnFailure requires user transactions to run inside the execution strategy.
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            dbContext.PickupTaskOrders.AddRange(result.Orders);
            dbContext.PickupTaskOrderItems.AddRange(result.Items);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.PickupTaskTraceLogs
                .Where(traceLog => traceLog.Id == log.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(traceLog => traceLog.IsProcessed, true), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
