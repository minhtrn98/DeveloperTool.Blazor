using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Services;

/// <summary>
/// Loads lightweight projections of the <c>pro.*_logs</c> tables (synced from SigNoz Pro by
/// <c>SignozProSyncJob</c>) for a Vietnam-calendar date range; aggregation happens in <see cref="DashboardAggregator"/>.
/// </summary>
public sealed class DashboardProStorageService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    /// <summary>How far past the range arrivals/completions are read so late-in-range manifests still get a lifecycle.</summary>
    private static readonly TimeSpan LifecycleLookAhead = TimeSpan.FromDays(2);

    public async Task<DashboardSnapshot> LoadAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        // Npgsql only accepts UTC offsets for timestamptz parameters.
        DateTimeOffset from = VietnamTimeHelper.FromVietnamLocal(startDate.ToDateTime(TimeOnly.MinValue)).ToUniversalTime();
        DateTimeOffset to = VietnamTimeHelper.FromVietnamLocal(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToUniversalTime();
        DateTimeOffset lookAheadTo = to + LifecycleLookAhead;

        var manifestCommitsTask = QueryAsync(db => db.DeliveryManifestCommitLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < to)
            .Select(x => new { x.LogTimestamp, x.Actor, x.DeliveryManifestCode }), cancellationToken);

        var sessionCommitsTask = QueryAsync(db => db.DeliverySessionCommitLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < to)
            .Select(x => new { x.LogTimestamp, x.Actor, x.ItemCount, x.ManifestCodes }), cancellationToken);

        var arrivalsTask = QueryAsync(db => db.DeliveryArrivalLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < lookAheadTo)
            .Select(x => new { x.LogTimestamp, x.Actor, x.DeliveryManifestCode, x.DistanceMeters, x.IsGpsValid, x.Superseded }), cancellationToken);

        var completesTask = QueryAsync(db => db.DeliveryTaskCompleteLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < lookAheadTo)
            .Select(x => new { x.LogTimestamp, x.Actor, x.DeliveryManifestCode, x.DeliveredCount, x.CollectedCod }), cancellationToken);

        var failuresTask = QueryAsync(db => db.DeliveryFailureLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < to)
            .Select(x => new { x.LogTimestamp, x.Actor, x.FailureType, x.ItemCount }), cancellationToken);

        var transfersTask = QueryAsync(db => db.DeliveryTransferLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < to)
            .Select(x => new { x.LogTimestamp, x.Actor, x.SourceType, x.OrderCount }), cancellationToken);

        var handoversTask = QueryAsync(db => db.UnloadingHandoverLogs
            .Where(x => x.LogTimestamp >= from && x.LogTimestamp < to)
            .Select(x => new { x.LogTimestamp, x.Actor, x.DriverId, x.ItemCount, x.ReceivedAt, x.ConfirmAt }), cancellationToken);

        await Task.WhenAll(manifestCommitsTask, sessionCommitsTask, arrivalsTask, completesTask, failuresTask, transfersTask, handoversTask);

        var manifestCommits = manifestCommitsTask.Result;
        var sessionCommits = sessionCommitsTask.Result;
        var arrivalsInRange = arrivalsTask.Result.Where(x => x.LogTimestamp < to).ToList();
        var completesInRange = completesTask.Result.Where(x => x.LogTimestamp < to).ToList();
        var failures = failuresTask.Result;
        var transfers = transfersTask.Result;
        var handovers = handoversTask.Result;

        List<DashboardEvent> events =
        [
            .. manifestCommits.Select(x => new DashboardEvent(DashboardEventType.ManifestCommit, x.LogTimestamp, x.Actor)),
            .. sessionCommits.Select(x => new DashboardEvent(DashboardEventType.SessionCommit, x.LogTimestamp, x.Actor, x.ItemCount)),
            .. arrivalsInRange.Select(x => new DashboardEvent(DashboardEventType.Arrival, x.LogTimestamp, x.Actor)),
            .. completesInRange.Select(x => new DashboardEvent(DashboardEventType.TaskComplete, x.LogTimestamp, x.Actor, x.DeliveredCount, x.CollectedCod)),
            .. failures.Select(x => new DashboardEvent(DashboardEventType.Failure, x.LogTimestamp, x.Actor, x.ItemCount)),
            .. transfers.Select(x => new DashboardEvent(DashboardEventType.Transfer, x.LogTimestamp, x.Actor, x.OrderCount)),
            .. handovers.Select(x => new DashboardEvent(DashboardEventType.UnloadingHandover, x.LogTimestamp, x.Actor, x.ItemCount))
        ];

        IEnumerable<ManifestEvent> createdManifests = manifestCommits
            .Select(x => new ManifestEvent(x.DeliveryManifestCode, x.LogTimestamp))
            .Concat(sessionCommits.SelectMany(x => x.ManifestCodes.Select(code => new ManifestEvent(code, x.LogTimestamp))));

        List<ManifestLifecycle> manifests = DashboardAggregator.BuildManifestLifecycles(
            createdManifests,
            arrivalsTask.Result.Select(x => new ManifestEvent(x.DeliveryManifestCode, x.LogTimestamp)),
            completesTask.Result.Select(x => new ManifestEvent(x.DeliveryManifestCode, x.LogTimestamp)));

        return new DashboardSnapshot(
            startDate,
            endDate,
            events,
            handovers.Select(x => new HandoverRecord(x.LogTimestamp, x.ReceivedAt, x.ConfirmAt, x.DriverId, x.ItemCount)).ToList(),
            arrivalsInRange.Select(x => new ArrivalRecord(x.DistanceMeters, x.IsGpsValid, x.Superseded != 0)).ToList(),
            CountBy(failures.Select(x => x.FailureType)),
            CountBy(transfers.Select(x => x.SourceType)),
            manifests);
    }

    private static List<LabelCount> CountBy(IEnumerable<string> labels)
        => labels
            .GroupBy(label => string.IsNullOrWhiteSpace(label) ? DashboardAggregator.UnknownActor : label)
            .Select(group => new LabelCount(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

    // Each query gets its own context so the table reads run in parallel.
    private async Task<List<T>> QueryAsync<T>(Func<ProApplicationDbContext, IQueryable<T>> query, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return await query(dbContext).ToListAsync(cancellationToken);
    }
}
