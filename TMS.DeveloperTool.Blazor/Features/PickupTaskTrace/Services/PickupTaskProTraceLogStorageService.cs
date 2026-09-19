using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;

/// <summary>
/// Read-only counterpart of <see cref="PickupTaskTraceLogStorageService"/> for the "pro" schema
/// (pickup task trace logs synced from SigNoz Pro by <c>SignozProSyncJob</c>).
/// </summary>
public sealed class PickupTaskProTraceLogStorageService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    public async Task<List<string>> GetDistinctPickupTaskIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.PickupTaskTraceLogs
            .AsNoTracking()
            .Where(x => x.PickupTaskId != string.Empty)
            .Select(x => x.PickupTaskId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctDeliveryLineIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.PickupTaskTraceLogs
            .AsNoTracking()
            .Where(x => x.DeliveryLineId != string.Empty)
            .Select(x => x.DeliveryLineId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<PickupTaskTraceLog> Items, int TotalCount)> QueryAsync(
        string? pickupTaskId,
        string? deliveryLineId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<PickupTaskTraceLog> query = dbContext.PickupTaskTraceLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(pickupTaskId))
        {
            query = query.Where(x => x.PickupTaskId == pickupTaskId);
        }

        if (!string.IsNullOrWhiteSpace(deliveryLineId))
        {
            query = query.Where(x => x.DeliveryLineId == deliveryLineId);
        }

        query = query.OrderByDescending(x => x.LogTimestamp);

        int totalCount = await query.CountAsync(cancellationToken);
        List<PickupTaskTraceLog> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
