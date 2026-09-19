using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

/// <summary>
/// Read-only counterpart of <see cref="OrderStep1TraceLogStorageService"/> for the "pro" schema
/// (order step1 trace logs synced from SigNoz Pro by <c>SignozProSyncJob</c>).
/// </summary>
public sealed class OrderStep1ProTraceLogStorageService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    public async Task<List<string>> GetDistinctOrderIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.OrderStep1TraceLogs
            .AsNoTracking()
            .Select(x => x.OrderId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OrderStep1TraceLog>> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.OrderStep1TraceLogs
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.LogTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<OrderStep1TraceLog> Items, int TotalCount)> QueryAsync(
        string? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<OrderStep1TraceLog> query = dbContext.OrderStep1TraceLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            query = query.Where(x => x.OrderId == orderId);
        }

        query = query.OrderByDescending(x => x.LogTimestamp);

        int totalCount = await query.CountAsync(cancellationToken);
        List<OrderStep1TraceLog> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
