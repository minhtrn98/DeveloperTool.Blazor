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

    public async Task<List<OrderItemJourneyDto>> GetOrderItemJourneyAsync(string orderItemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                dm.code as "ManifestCode",
                ds.driver_code as "DriverCode",
                'Đi phát' as "Operation",
                'Lên hàng' as "Action",
                ds.created_at as "ActionAt",
                ds.vehicle_license_plate as "VehicleLicensePlate",
                ds.assignment_code as "AssignmentCode",
                ds.plan_code as "PlanCode",
                ds.loading_handover_id::text as "HandoverId",
                ds.pickup_gps_lat as "Lat",
                ds.pickup_gps_lng as "Lng",
                ds.pickup_stop_code as "StopCode",
                ds.pickup_stop_name as "StopName",
                ds.pickup_misplaced_reason as "MisplacedReason",
                ds.pickup_misplaced_reason_name as "MisplacedReasonName"
            from public.delivery_items di
            join public.delivery_manifests dm on di.delivery_manifest_id = dm.id and di.manifest_created_at = dm.created_at
            join public.delivery_sessions ds on ds.id = di.session_id and ds.created_at = di.session_created_at
            where di.order_item_id = @OrderItemId

            union all

            select
                dm.code as "ManifestCode",
                ds.driver_code as "DriverCode",
                'Đi phát' as "Operation",
                case
                    when dt.completed_at is null and dt.delivered_at is null then 'Đang phát'
                    when dt.delivered_at is not null then 'Phát thành công'
                    else 'Trả hàng'
                end as "Action",
                coalesce(dt.delivered_at, dt.completed_at) as "ActionAt",
                ds.vehicle_license_plate as "VehicleLicensePlate",
                ds.assignment_code as "AssignmentCode",
                ds.plan_code as "PlanCode",
                dt.unloading_handover_id::text as "HandoverId",
                null::double precision as "Lat",
                null::double precision as "Lng",
                dt.actual_dropoff_stop_code as "StopCode",
                case
                    when dt.delivered_at is not null and dt.misplaced_dropoff_reason is null then dt.receiver_address
                    when dt.completed_at is null then null
                    else dt.actual_dropoff_stop_name
                end as "StopName",
                dt.misplaced_dropoff_reason as "MisplacedReason",
                dt.misplaced_dropoff_reason_name as "MisplacedReasonName"
            from public.delivery_items di
            join public.delivery_manifests dm on di.delivery_manifest_id = dm.id and di.manifest_created_at = dm.created_at
            join public.delivery_tasks dt on dt.id = di.delivery_task_id and dt.created_at = di.task_created_at
            join public.delivery_sessions ds on ds.id = di.session_id and ds.created_at = di.session_created_at
            where di.order_item_id = @OrderItemId

            order by "ActionAt"
            """;

        IEnumerable<OrderItemJourneyDto> rows = await dbQuery.QueryAsync<OrderItemJourneyDto>(sql, new { OrderItemId = orderItemId }, cancellationToken);
        return [.. rows];
    }
}
