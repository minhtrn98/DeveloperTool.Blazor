namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class ManifestRepository([FromKeyedServices("OrderDb")] ApplicationDbQuery dbQuery)
{
    public async Task<List<DeliveryManifest>> GetDeliveryManifestsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                dm.id as "Id",
                dm.created_at as "CreatedAt",
                dm.code as "Code",
                dm.manifest_type as "ManifestType",
                dm.delivery_mode as "DeliveryMode",
                dm.status as "Status",
                dm.origin_post_office_name as "OriginPostOfficeName",
                dm.created_by_name as "CreatedByName",
                dm.total_sessions as "TotalSessions",
                dm.total_orders as "TotalOrders",
                dm.total_items as "TotalItems",
                dm.delivered_items as "DeliveredItems",
                dm.failed_items as "FailedItems",
                dm.loaded_items as "LoadedItems",
                dm.total_cod_amount as "TotalCodAmount",
                dm.collected_cod_amount as "CollectedCodAmount",
                dm.completed_at as "CompletedAt",
                dm.cancelled_at as "CancelledAt"
            from public.delivery_manifests dm
            where dm.is_deleted = false
            order by dm.created_at desc
            """;

        IEnumerable<DeliveryManifest> manifests = await dbQuery.QueryAsync<DeliveryManifest>(sql, null, cancellationToken);
        return [.. manifests];
    }

    public async Task<List<DeliveryManifestSessionDto>> GetDeliveryManifestSessionsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        DateTimeOffset startOfDay = new DateTimeOffset(date.Date, TimeSpan.FromHours(7)).ToUniversalTime();
        DateTimeOffset endOfDay = startOfDay.AddDays(1);

        const string sqlStats = """
            select
                i.delivery_manifest_id as "DeliveryManifestId",
                count(*) as "Total",
                count(*) filter (where i.status in (100, 101)) as "Delivering",
                count(*) filter (where i.is_loaded) as "OnVehicle",
                count(*) filter (where i.status = 6) as "Delivered",
                count(*) filter (where i.status = 13) as "DeliveryRescheduled",
                count(*) filter (where i.status = 16) as "PendingProcessing",
                count(*) filter (where i.status = 7) as "Returning",
                count(*) filter (where i.status = 9) as "Forwarded",
                count(*) filter (where i.status = 19) as "Lost",
                count(*) filter (where i.status = 24) as "Confiscated",
                count(*) filter (where i.status = 18) as "Destroyed"
            from public.delivery_items i
            where i.manifest_created_at >= @Start and i.manifest_created_at < @End
              and i.is_active
            group by i.delivery_manifest_id
            order by i.delivery_manifest_id;
            """;

        const string sqlManifests = """
            select distinct
                m.id as "Id",
                m.code as "Code",
                m.status as "Status",
                m.plan_code as "PlanCode",
                s.pairing_code as "PairingCode",
                s.driver_code as "DriverCode",
                s.vehicle_license_plate as "VehicleLicensePlate",
                m.total_items as "TotalItems",
                m.total_completed as "TotalCompleted"
            from public.delivery_manifests m
            join public.delivery_sessions s on s.delivery_manifest_id = m.id
            where m.created_at >= @Start and m.created_at < @End
            order by m.code;
            """;

        IEnumerable<DeliveryItemStatsDto> stats = await dbQuery.QueryAsync<DeliveryItemStatsDto>(sqlStats, new { Start = startOfDay, End = endOfDay }, cancellationToken);
        IEnumerable<DeliveryManifestSessionDto> manifests = await dbQuery.QueryAsync<DeliveryManifestSessionDto>(sqlManifests, new { Start = startOfDay, End = endOfDay }, cancellationToken);

        List<DeliveryManifestSessionDto> result = [.. manifests];
        foreach (DeliveryManifestSessionDto manifest in result)
        {
            manifest.Stats = stats.FirstOrDefault(s => s.DeliveryManifestId == manifest.Id);
        }

        return result;
    }
}
