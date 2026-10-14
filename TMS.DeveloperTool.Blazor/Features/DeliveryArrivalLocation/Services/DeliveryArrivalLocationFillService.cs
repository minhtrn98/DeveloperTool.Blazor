using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.DeliveryArrivalLocation.Models;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

namespace TMS.DeveloperTool.Blazor.Features.DeliveryArrivalLocation.Services;

/// <summary>
/// Fills the vehicle and order positions of <c>pro.delivery_arrival_logs</c> from data the sync
/// job stored as-is: the vehicle position from the same-trace "DeliveryArrival" route stop, and the
/// order's configured position from <c>pro.order_location_logs</c>. Safe to re-run. Used by
/// <see cref="DeliveryArrivalLocationFillJob"/> and the manual "Fill vị trí check-in" button.
/// </summary>
public sealed class DeliveryArrivalLocationFillService(
    IDbContextFactory<ProApplicationDbContext> dbContextFactory,
    DeliveryArrivalLocationFillOptions options)
{
    // route_stop_trace_logs.action of the route stop recorded with each [RecordDeliveryArrival]
    // (1:1 by trace id); its message detail carries the vehicle's Latitude/Longitude.
    private const string DeliveryArrivalRouteStopAction = "DeliveryArrival";

    private const int BatchSize = 500;

    // The job and the button share this singleton; one fill at a time, the other one waits.
    private readonly SemaphoreSlim _fillLock = new(1, 1);

    /// <summary>
    /// Fills vehicle then order positions for arrivals in the last
    /// <see cref="DeliveryArrivalLocationFillOptions.LookbackDays"/>.
    /// </summary>
    public async Task<DeliveryArrivalLocationFillResult> FillAsync(CancellationToken cancellationToken)
    {
        await _fillLock.WaitAsync(cancellationToken);
        try
        {
            DateTimeOffset sinceUtc = DateTimeOffset.UtcNow.AddDays(-options.LookbackDays);
            int vehicleFilled = await FillVehicleLocationAsync(sinceUtc, cancellationToken);
            int orderFilled = await FillOrderLocationAsync(sinceUtc, cancellationToken);
            return new DeliveryArrivalLocationFillResult(sinceUtc, vehicleFilled, orderFilled);
        }
        finally
        {
            _fillLock.Release();
        }
    }

    /// <summary>
    /// Sets <c>vehicle_lat</c>/<c>vehicle_lng</c> on arrivals since <paramref name="sinceUtc"/> that
    /// don't have them yet and whose route stop is synced. Returns how many arrivals were filled.
    /// </summary>
    public async Task<int> FillVehicleLocationAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        int filled = 0;
        long lastId = 0;
        while (true)
        {
            var batch = await dbContext.DeliveryArrivalLogs
                .AsNoTracking()
                .Where(arrival => arrival.Id > lastId
                    && arrival.LogTimestamp >= sinceUtc
                    && arrival.VehicleLat == null
                    && arrival.VehicleLng == null)
                .Join(
                    dbContext.RouteStopTraceLogs.Where(stop => stop.Action == DeliveryArrivalRouteStopAction),
                    arrival => arrival.TraceId,
                    stop => stop.TraceId,
                    (arrival, stop) => new { arrival.Id, stop.MessageDetail })
                .OrderBy(row => row.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var row in batch)
            {
                // Parsed here rather than in SQL so one malformed message can't fail the whole run;
                // rows without coordinates stay null and are simply looked at again next run.
                (decimal? latitude, decimal? longitude) = RouteStopMessageFieldsParser.ParseLocation(row.MessageDetail);
                if (latitude is null && longitude is null)
                {
                    continue;
                }

                filled += await dbContext.DeliveryArrivalLogs
                    .Where(arrival => arrival.Id == row.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(arrival => arrival.VehicleLat, latitude)
                        .SetProperty(arrival => arrival.VehicleLng, longitude), cancellationToken);
            }

            if (batch.Count < BatchSize)
            {
                return filled;
            }

            lastId = batch[^1].Id;
        }
    }

    /// <summary>
    /// Sets <c>order_lat</c>/<c>order_lng</c>/<c>order_location_at</c> on arrivals since
    /// <paramref name="sinceUtc"/> to the order's location as it was at check-in: the latest
    /// <c>order_location_logs</c> row at or before the arrival, else the first one after it.
    /// Re-evaluated every run, so a location synced late still wins. Returns how many arrivals changed.
    /// </summary>
    public async Task<int> FillOrderLocationAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            WITH best AS (
                SELECT a.id, l.lat, l.lng, l.log_timestamp
                FROM pro.delivery_arrival_logs a
                CROSS JOIN LATERAL (
                    SELECT ol.lat, ol.lng, ol.log_timestamp
                    FROM pro.order_location_logs ol
                    WHERE ol.order_id = a.order_id
                    ORDER BY (ol.log_timestamp <= a.log_timestamp) DESC,
                             CASE WHEN ol.log_timestamp <= a.log_timestamp THEN ol.log_timestamp END DESC,
                             ol.log_timestamp
                    LIMIT 1
                ) l
                WHERE a.log_timestamp >= {sinceUtc} AND a.order_id <> ''
            )
            UPDATE pro.delivery_arrival_logs a
            SET order_lat = best.lat, order_lng = best.lng, order_location_at = best.log_timestamp
            FROM best
            WHERE a.id = best.id AND a.order_location_at IS DISTINCT FROM best.log_timestamp;
            """, cancellationToken);
    }
}
