using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Models;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

public sealed class RouteStopTraceLogStorageService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<bool> SaveIfNewAsync(RouteStopTraceLogEntry entry, CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO route_stop_trace_logs (log_id, trace_id, span_id, log_timestamp, action, driver, office, event_id, vehicle_id, assignment_id, message_detail, created_at)
            VALUES ({entry.LogId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.Action}, {entry.Driver}, {entry.Office}, {entry.EventId}, {entry.VehicleId}, {entry.AssignmentId}, {entry.MessageDetail}, {DateTimeOffset.UtcNow})
            ON CONFLICT (log_id) DO NOTHING;
            """, cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<List<Guid>> GetDistinctVehicleIdsAsync(CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> ids = await dbContext.RouteStopTraceLogs
            .AsNoTracking()
            .Where(x => x.VehicleId != string.Empty)
            .Select(x => x.VehicleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ParseGuids(ids);
    }

    public async Task<List<Guid>> GetDistinctAssignmentIdsAsync(CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> ids = await dbContext.RouteStopTraceLogs
            .AsNoTracking()
            .Where(x => x.AssignmentId != string.Empty)
            .Select(x => x.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ParseGuids(ids);
    }

    private static List<Guid> ParseGuids(List<string> ids)
    {
        return ids
            .Select(id => Guid.TryParse(id, out Guid guid) ? guid : (Guid?)null)
            .Where(guid => guid.HasValue)
            .Select(guid => guid!.Value)
            .ToList();
    }

    public async Task<(List<RouteStopTraceLog> Items, int TotalCount)> QueryAsync(
        string? vehicleId,
        string? assignmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<RouteStopTraceLog> query = dbContext.RouteStopTraceLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(vehicleId))
        {
            query = query.Where(x => x.VehicleId == vehicleId);
        }

        if (!string.IsNullOrWhiteSpace(assignmentId))
        {
            query = query.Where(x => x.AssignmentId == assignmentId);
        }

        query = query.OrderByDescending(x => x.LogTimestamp);

        int totalCount = await query.CountAsync(cancellationToken);
        List<RouteStopTraceLog> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
