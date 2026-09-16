using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;

public sealed class PickupTaskTraceLogStorageService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<bool> SaveIfNewAsync(PickupTaskTraceLogEntry entry, CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO pickup_task_trace_logs (log_id, trace_id, span_id, log_timestamp, pickup_task_id, delivery_line_id, event_id, message_detail, created_at)
            VALUES ({entry.LogId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.PickupTaskId}, {entry.DeliveryLineId}, {entry.EventId}, {entry.MessageDetail}, {DateTimeOffset.UtcNow})
            ON CONFLICT (log_id) DO NOTHING;
            """, cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<List<string>> GetDistinctPickupTaskIdsAsync(CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
