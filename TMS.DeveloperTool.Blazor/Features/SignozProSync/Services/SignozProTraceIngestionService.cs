using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Models;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;
using TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;

/// <summary>
/// Persists trace entries pulled from SigNoz Pro into the "pro" schema, tagged with a fixed
/// "Production" env — this data always originates from the Production SigNoz instance,
/// regardless of which environment this tool itself is deployed to.
/// </summary>
/// <remarks>
/// Callers own one <see cref="ProApplicationDbContext"/> for the whole sync run (see
/// <see cref="CreateContextAsync"/>) instead of one per row, and are expected to call
/// <see cref="ClearTracking"/> after each page so the context doesn't accumulate state across a
/// long-running backfill. Inserts still go through raw SQL with
/// <c>ON CONFLICT (log_id) DO NOTHING</c> rather than tracked <c>Add</c> calls, because the sync
/// job deliberately re-queries a small overlap window every run and relies on that upsert
/// semantic to make the overlap harmless.
/// </remarks>
public sealed class SignozProTraceIngestionService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    private const string Env = "Production";

    public Task<ProApplicationDbContext> CreateContextAsync(CancellationToken cancellationToken)
        => dbContextFactory.CreateDbContextAsync(cancellationToken);

    public static void ClearTracking(ProApplicationDbContext dbContext) => dbContext.ChangeTracker.Clear();

    /// <summary>Convenience overload for the Order Step 1 Pro trace page, which saves one polled entry at a time.</summary>
    public async Task<bool> SaveIfNewAsync(OrderStep1TraceLogEntry entry, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await CreateContextAsync(cancellationToken);
        int savedCount = await SaveIfNewAsync(dbContext, [entry], cancellationToken);
        return savedCount > 0;
    }

    public async Task<int> SaveIfNewAsync(ProApplicationDbContext dbContext, IReadOnlyList<OrderStep1TraceLogEntry> entries, CancellationToken cancellationToken)
    {
        int savedCount = 0;
        foreach (OrderStep1TraceLogEntry entry in entries)
        {
            int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO pro.order_step1_trace_logs (log_id, order_id, trace_id, span_id, log_timestamp, message_detail, env, created_at)
                VALUES ({entry.LogId}, {entry.OrderId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.MessageDetail}, {Env}, {DateTimeOffset.UtcNow})
                ON CONFLICT (log_id) DO NOTHING;
                """, cancellationToken);

            if (rowsAffected > 0)
            {
                savedCount++;
            }
        }

        return savedCount;
    }

    public async Task<int> SaveIfNewAsync(ProApplicationDbContext dbContext, IReadOnlyList<RouteStopTraceLogEntry> entries, CancellationToken cancellationToken)
    {
        int savedCount = 0;
        foreach (RouteStopTraceLogEntry entry in entries)
        {
            string actionObjectId = RouteStopMessageFieldsParser.Parse(entry.MessageDetail).ActionObjectId;

            int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO pro.route_stop_trace_logs (log_id, trace_id, span_id, log_timestamp, action, driver, office, event_id, vehicle_id, assignment_id, action_object_id, message_detail, env, created_at)
                VALUES ({entry.LogId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.Action}, {entry.Driver}, {entry.Office}, {entry.EventId}, {entry.VehicleId}, {entry.AssignmentId}, {actionObjectId}, {entry.MessageDetail}, {Env}, {DateTimeOffset.UtcNow})
                ON CONFLICT (log_id) DO NOTHING;
                """, cancellationToken);

            if (rowsAffected > 0)
            {
                savedCount++;
            }
        }

        return savedCount;
    }

    public async Task<int> SaveIfNewAsync(ProApplicationDbContext dbContext, IReadOnlyList<DeliveryManifestCommitLogEntry> entries, CancellationToken cancellationToken)
    {
        int savedCount = 0;
        foreach (DeliveryManifestCommitLogEntry entry in entries)
        {
            int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO pro.delivery_manifest_commit_logs (log_id, trace_id, span_id, log_timestamp, delivery_manifest_code, cod_manifest_code, actor, env, created_at)
                VALUES ({entry.LogId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.DeliveryManifestCode}, {entry.CodManifestCode}, {entry.Actor}, {Env}, {DateTimeOffset.UtcNow})
                ON CONFLICT (log_id) DO NOTHING;
                """, cancellationToken);

            if (rowsAffected > 0)
            {
                savedCount++;
            }
        }

        return savedCount;
    }

    public async Task<int> SaveIfNewAsync(ProApplicationDbContext dbContext, IReadOnlyList<PickupTaskTraceLogEntry> entries, CancellationToken cancellationToken)
    {
        int savedCount = 0;
        foreach (PickupTaskTraceLogEntry entry in entries)
        {
            int rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO pro.pickup_task_trace_logs (log_id, trace_id, span_id, log_timestamp, pickup_task_id, delivery_line_id, event_id, message_detail, env, created_at)
                VALUES ({entry.LogId}, {entry.TraceId}, {entry.SpanId}, {entry.Timestamp.ToUniversalTime()}, {entry.PickupTaskId}, {entry.DeliveryLineId}, {entry.EventId}, {entry.MessageDetail}, {Env}, {DateTimeOffset.UtcNow})
                ON CONFLICT (log_id) DO NOTHING;
                """, cancellationToken);

            if (rowsAffected > 0)
            {
                savedCount++;
            }
        }

        return savedCount;
    }
}
