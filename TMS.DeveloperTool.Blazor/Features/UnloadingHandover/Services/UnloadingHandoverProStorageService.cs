using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.UnloadingHandover.Models;

namespace TMS.DeveloperTool.Blazor.Features.UnloadingHandover.Services;

/// <summary>
/// Read-only queries over <c>pro.unloading_handover_logs</c> (synced from SigNoz Pro by
/// <c>SignozProSyncJob</c>) for handovers an NVKT hasn't received and/or confirmed yet.
/// </summary>
public sealed class UnloadingHandoverProStorageService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    public async Task<List<string>> GetDistinctPendingDriverIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.UnloadingHandoverLogs
            .AsNoTracking()
            .Where(x => (!x.IsReceived || !x.IsConfirm) && x.DriverId != string.Empty)
            .Select(x => x.DriverId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Oldest first — the handovers that have been waiting longest are the most actionable.</summary>
    public async Task<(List<UnloadingHandoverLog> Items, int TotalCount)> QueryPendingAsync(
        string? status,
        string? driverId,
        string? handoverCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<UnloadingHandoverLog> query = dbContext.UnloadingHandoverLogs.AsNoTracking();

        query = status switch
        {
            UnloadingHandoverPendingStatus.NotReceived => query.Where(x => !x.IsReceived),
            UnloadingHandoverPendingStatus.NotConfirmed => query.Where(x => !x.IsConfirm),
            _ => query.Where(x => !x.IsReceived || !x.IsConfirm)
        };

        if (!string.IsNullOrWhiteSpace(driverId))
        {
            query = query.Where(x => x.DriverId == driverId);
        }

        if (!string.IsNullOrWhiteSpace(handoverCode))
        {
            string pattern = $"%{handoverCode.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.HandoverCode, pattern));
        }

        query = query.OrderBy(x => x.LogTimestamp);

        int totalCount = await query.CountAsync(cancellationToken);
        List<UnloadingHandoverLog> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
