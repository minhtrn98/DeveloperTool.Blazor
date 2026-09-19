using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

/// <summary>
/// Read-only counterpart of <see cref="RouteStopTraceLogStorageService"/> for the "pro" schema
/// (route stop trace logs synced from SigNoz Pro by <c>SignozProSyncJob</c>).
/// </summary>
public sealed class RouteStopProTraceLogStorageService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    public async Task<List<Guid>> GetDistinctVehicleIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> ids = await dbContext.RouteStopTraceLogs
            .AsNoTracking()
            .Where(x => x.VehicleId != string.Empty)
            .Select(x => x.VehicleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ParseGuids(ids);
    }

    /// <summary>Used only to look up Assignment Code for display in the "Assignment Code" column — not exposed as a filter.</summary>
    public async Task<List<Guid>> GetDistinctAssignmentIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> ids = await dbContext.RouteStopTraceLogs
            .AsNoTracking()
            .Where(x => x.AssignmentId != string.Empty)
            .Select(x => x.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ParseGuids(ids);
    }

    public async Task<List<string>> GetDistinctActionObjectIdsAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.RouteStopTraceLogs
            .AsNoTracking()
            .Where(x => x.ActionObjectId != string.Empty)
            .Select(x => x.ActionObjectId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
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
        string? actionObjectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<RouteStopTraceLog> query = dbContext.RouteStopTraceLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(vehicleId))
        {
            query = query.Where(x => x.VehicleId == vehicleId);
        }

        if (!string.IsNullOrWhiteSpace(actionObjectId))
        {
            query = query.Where(x => x.ActionObjectId == actionObjectId);
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
