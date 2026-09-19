namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;

/// <summary>
/// Every 5 minutes, pulls the last window of RouteStop/PickupTask trace logs from SigNoz Pro
/// (the Production monitor) and stores any new rows into the "pro" schema. Each page returned
/// by SigNoz is saved as soon as it arrives (instead of buffering the whole window in memory),
/// and the shared DbContext's change tracker is cleared right after — keeps a long backfill
/// (e.g. from a configured <see cref="SignozProOptions.QueryFromDate"/>) flat on memory.
/// </summary>
/// <remarks>
/// OrderStep1 is deliberately not synced here — SigNoz has too many OrderStep1 events to poll
/// in bulk without an order-id filter. It's traced on demand instead, one order id at a time,
/// from the "Order Step 1" Pro trace page (see <see cref="SignozProQueryService.QueryOrderStep1ByOrderIdAsync"/>).
/// </remarks>
public sealed class SignozProSyncJob(
    IServiceScopeFactory scopeFactory,
    SignozProSyncCheckpointStore checkpointStore,
    SignozProOptions signozProOptions,
    ILogger<SignozProSyncJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    // Used only as the query start when no checkpoint file exists yet and QueryFromDate isn't
    // configured either. Every run after that resumes from the saved checkpoint instead.
    private static readonly TimeSpan FallbackLookbackWindow = TimeSpan.FromMinutes(6);

    // Subtracted from "now" before saving the checkpoint, so the next run re-queries a small
    // overlap instead of a hard cutoff — covers logs that were still being ingested by SigNoz
    // at the moment this run queried. ON CONFLICT (log_id) DO NOTHING makes the overlap harmless.
    private static readonly TimeSpan CheckpointSafetyBuffer = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);
        do
        {
            try
            {
                await SyncOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "SigNoz Pro trace sync failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SyncOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        SignozProQueryService queryService = scope.ServiceProvider.GetRequiredService<SignozProQueryService>();
        SignozProTraceIngestionService ingestionService = scope.ServiceProvider.GetRequiredService<SignozProTraceIngestionService>();

        DateTimeOffset end = DateTimeOffset.UtcNow;
        DateTimeOffset? checkpoint = await checkpointStore.ReadAsync(cancellationToken);
        DateTimeOffset start = checkpoint ?? signozProOptions.QueryFromDate ?? end - FallbackLookbackWindow;

        if (start >= end)
        {
            logger.LogInformation("SigNoz Pro trace sync: checkpoint ({Checkpoint}) is not before now, skipping this tick.", start);
            return;
        }

        await using ProApplicationDbContext dbContext = await ingestionService.CreateContextAsync(cancellationToken);

        int routeStopSaved = 0;
        int routeStopFetched = await queryService.QueryRouteStopAsync(start, end, async (page, ct) =>
        {
            routeStopSaved += await ingestionService.SaveIfNewAsync(dbContext, page, ct);
            SignozProTraceIngestionService.ClearTracking(dbContext);
        }, cancellationToken);

        int pickupTaskSaved = 0;
        int pickupTaskFetched = await queryService.QueryPickupTaskAsync(start, end, async (page, ct) =>
        {
            pickupTaskSaved += await ingestionService.SaveIfNewAsync(dbContext, page, ct);
            SignozProTraceIngestionService.ClearTracking(dbContext);
        }, cancellationToken);

        DateTimeOffset newCheckpoint = end - CheckpointSafetyBuffer;
        if (newCheckpoint > start)
        {
            await checkpointStore.SaveAsync(newCheckpoint, cancellationToken);
        }

        logger.LogInformation(
            "SigNoz Pro trace sync [{Start} - {End}]: {RouteStopCount} route-stop ({RouteStopSaved} new), " +
            "{PickupTaskCount} pickup-task ({PickupTaskSaved} new).",
            start, end,
            routeStopFetched, routeStopSaved,
            pickupTaskFetched, pickupTaskSaved);
    }
}
