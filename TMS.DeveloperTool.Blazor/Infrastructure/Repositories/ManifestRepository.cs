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
                null as "ExternalId",
                null as "PickupTaskId",
                null as "ConfirmedPickupTaskId",
                ds.driver_code as "DriverCode",
                'Đi phát' as "Operation",
                'Lên hàng' as "Action",
                ds.created_at as "ActionAt",
                ds.vehicle_license_plate as "VehicleLicensePlate",
                ds.assignment_code as "AssignmentCode",
                ds.plan_code as "PlanCode",
                ds.loading_handover_id::text as "HandoverId",
                h.code as "HandoverCode",
                ds.pickup_gps_lat as "Lat",
                ds.pickup_gps_lng as "Lng",
                ds.pickup_stop_code as "StopCode",
                ds.pickup_stop_name as "StopName",
                ds.pickup_misplaced_reason as "MisplacedReason",
                ds.pickup_misplaced_reason_name as "MisplacedReasonName"
            from public.delivery_items di
            join public.delivery_manifests dm on di.delivery_manifest_id = dm.id and di.manifest_created_at = dm.created_at
            join public.delivery_sessions ds on ds.id = di.session_id and ds.created_at = di.session_created_at
            left join public.handovers h on h.id = ds.loading_handover_id
            where di.order_item_id = @OrderItemId

            union all

            select
                dm.code as "ManifestCode",
                null as "ExternalId",
                null as "PickupTaskId",
                null as "ConfirmedPickupTaskId",
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
                h.code as "HandoverCode",
                null::double precision as "Lat",
                null::double precision as "Lng",
                dt.actual_dropoff_stop_code as "StopCode",
                dt.actual_dropoff_stop_name as "StopName",
                dt.misplaced_dropoff_reason as "MisplacedReason",
                dt.misplaced_dropoff_reason_name as "MisplacedReasonName"
            from public.delivery_items di
            join public.delivery_manifests dm on di.delivery_manifest_id = dm.id and di.manifest_created_at = dm.created_at
            join public.delivery_tasks dt on dt.id = di.delivery_task_id and dt.created_at = di.task_created_at
            join public.delivery_sessions ds on ds.id = di.session_id and ds.created_at = di.session_created_at
            left join public.handovers h on h.id = dt.unloading_handover_id
            where di.order_item_id = @OrderItemId

            union all

            select
                null as "ManifestCode",
                li.parent_item_external_id as "ExternalId",
                null as "PickupTaskId",
                null as "ConfirmedPickupTaskId",
                ps.driver_code as "DriverCode",
                'Kết nối' as "Operation",
                'Lên hàng' as "Action",
                ps.created_at as "ActionAt",
                ps.vehicle_license_plate as "VehicleLicensePlate",
                null as "AssignmentCode",
                null as "PlanCode",
                li.loaded_handover_id::text as "HandoverId",
                h.code as "HandoverCode",
                ps.pickup_gps_lat as "Lat",
                ps.pickup_gps_lng as "Lng",
                ps.pickup_stop_code as "StopCode",
                ps.pickup_stop_name as "StopName",
                ps.pickup_misplaced_reason as "MisplacedReason",
                ps.pickup_misplaced_reason_name as "MisplacedReasonName"
            from public.linehaul_items li
            join public.linehaul_pickup_sessions ps on ps.id = li.session_id and ps.created_at = li.session_created_at
            left join public.handovers h on h.id = li.loaded_handover_id
            where li.order_item_id = @OrderItemId

            union all

            select
                null as "ManifestCode",
                li.parent_item_external_id as "ExternalId",
                null as "PickupTaskId",
                null as "ConfirmedPickupTaskId",
                ps.driver_code as "DriverCode",
                'Kết nối' as "Operation",
                'Trả hàng' as "Action",
                li.actual_unloaded_at as "ActionAt",
                ps.vehicle_license_plate as "VehicleLicensePlate",
                null as "AssignmentCode",
                null as "PlanCode",
                li.unloaded_handover_id::text as "HandoverId",
                h.code as "HandoverCode",
                null::double precision as "Lat",
                null::double precision as "Lng",
                li.actual_dropoff_stop_code as "StopCode",
                li.actual_dropoff_stop_name as "StopName",
                li.misplaced_dropoff_reason as "MisplacedReason",
                li.misplaced_dropoff_reason_name as "MisplacedReasonName"
            from public.linehaul_items li
            join public.linehaul_pickup_sessions ps on ps.id = li.session_id and ps.created_at = li.session_created_at
            join public.handovers h on h.id = li.unloaded_handover_id
            where li.order_item_id = @OrderItemId

            union all

            select
                null as "ManifestCode",
                null as "ExternalId",
                pi.pickup_task_id as "PickupTaskId",
                pi.confirmed_pickup_task_id as "ConfirmedPickupTaskId",
                ps.driver_code as "DriverCode",
                'Đi nhận' as "Operation",
                'Lên hàng' as "Action",
                pi.pickup_task_round_created_at as "ActionAt",
                ps.vehicle_license_plate as "VehicleLicensePlate",
                null as "AssignmentCode",
                ps.plan_code as "PlanCode",
                pi.loaded_handover_id::text as "HandoverId",
                h.code as "HandoverCode",
                pt.pickup_latitude as "Lat",
                pt.pickup_longitude as "Lng",
                null as "StopCode",
                pt.pickup_address as "StopName",
                null as "MisplacedReason",
                null as "MisplacedReasonName"
            from public.pickup_task_actual_picked_order_items pi
            join public.pickup_task_assigned_info ps on ps.pickup_task_id = pi.confirmed_pickup_task_id and ps.task_created_at = pi.confirmed_task_created_at and ps.is_active
            join public.pickup_tasks pt on pt.pickup_task_id = pi.confirmed_pickup_task_id and pt.created_at = pi.confirmed_task_created_at
            left join public.handovers h on h.id = pi.loaded_handover_id
            where pi.picked_order_item_id = @OrderItemId

            union all

            select
                null as "ManifestCode",
                null as "ExternalId",
                pi.pickup_task_id as "PickupTaskId",
                pi.confirmed_pickup_task_id as "ConfirmedPickupTaskId",
                ps.driver_code as "DriverCode",
                'Đi nhận' as "Operation",
                'Trả hàng' as "Action",
                pi.actual_dropped_at_post_office_at as "ActionAt",
                ps.vehicle_license_plate as "VehicleLicensePlate",
                null as "AssignmentCode",
                ps.plan_code as "PlanCode",
                pi.unloaded_handover_id::text as "HandoverId",
                h.code as "HandoverCode",
                null::double precision as "Lat",
                null::double precision as "Lng",
                pi.actual_dropoff_post_office_code as "StopCode",
                pi.actual_dropoff_post_office_name as "StopName",
                pi.misplaced_dropoff_reason as "MisplacedReason",
                pi.misplaced_dropoff_reason_name as "MisplacedReasonName"
            from public.pickup_task_actual_picked_order_items pi
            join public.pickup_task_assigned_info ps on ps.pickup_task_id = pi.confirmed_pickup_task_id and ps.task_created_at = pi.confirmed_task_created_at and ps.is_active
            left join public.handovers h on h.id = pi.unloaded_handover_id
            where pi.picked_order_item_id = @OrderItemId

            order by "ActionAt"
            """;

        IEnumerable<OrderItemJourneyDto> rows = await dbQuery.QueryAsync<OrderItemJourneyDto>(sql, new { OrderItemId = orderItemId }, cancellationToken);
        return [.. rows];
    }

    public async Task<OrderSummaryDto?> GetOrderSummaryByOrderItemIdAsync(string orderItemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                o.order_id as "OrderId",
                o.current_status_name as "CurrentStatusName",
                o.created_at as "CreatedAt",
                o.extra_service_name as "ExtraService",
                o.service_type_name as "ServiceType",
                o.pickup_task_id as "PickupTaskId",
                o.weight as "Weight"
            from public.orders o
            join public.order_items oi on oi.order_id = o.order_id
            where oi.order_item_id = @OrderItemId
            """;

        return await dbQuery.SingleOrDefaultAsync<OrderSummaryDto>(sql, new { OrderItemId = orderItemId }, cancellationToken);
    }

    public async Task<List<HandoverItemDto>> GetHandoverItemsAsync(string handoverId, string operation, string action, CancellationToken cancellationToken = default)
    {
        string sql;
        if (operation == "Kết nối" && action == "Lên hàng")
        {
            sql = """
                select
                    i.order_id as "OrderId",
                    i.order_item_id as "OrderItemId",
                    null::text as "ExtraService",
                    i.weight as "Weight",
                    null::smallint as "OrderType",
                    i.root_mail_trip_external_id as "RootMailTripExternalId"
                from public.handovers h
                join public.handover_linked_tasks ht on h.id = ht.handover_id
                join public.linehaul_items i on ht.task_id::uuid = i.session_id
                where h.id = @HandoverId::uuid and i.order_id is not null
                order by i.order_id, i.order_item_id
                """;
        }
        else if (operation == "Kết nối")
        {
            sql = """
                select
                    i.order_id as "OrderId",
                    i.order_item_id as "OrderItemId",
                    null::text as "ExtraService",
                    i.weight as "Weight",
                    null::smallint as "OrderType",
                    i.root_mail_trip_external_id as "RootMailTripExternalId"
                from public.linehaul_items i
                where i.unloaded_handover_id = @HandoverId::uuid and i.order_id is not null
                order by i.order_id, i.order_item_id
                """;
        }
        else if (operation == "Đi nhận")
        {
            string handoverColumn = action == "Lên hàng" ? "loaded_handover_id" : "unloaded_handover_id";
            sql = $"""
                select
                    i.picked_order_id as "OrderId",
                    i.picked_order_item_id as "OrderItemId",
                    null::text as "ExtraService",
                    i.weight as "Weight",
                    null::smallint as "OrderType",
                    null::text as "RootMailTripExternalId"
                from public.pickup_task_actual_picked_order_items i
                where i.{handoverColumn} = @HandoverId::uuid
                order by i.picked_order_id, i.picked_order_item_id
                """;
        }
        else
        {
            string joinColumn = action == "Lên hàng" ? "session_id" : "delivery_task_id";
            sql = $"""
                select
                    i.order_id as "OrderId",
                    i.order_item_id as "OrderItemId",
                    i.extra_service as "ExtraService",
                    i.weight as "Weight",
                    i.order_type as "OrderType",
                    null::text as "RootMailTripExternalId"
                from public.handovers h
                join public.handover_linked_tasks ht on h.id = ht.handover_id
                join public.delivery_items i on ht.task_id::uuid = i.{joinColumn}
                where h.id = @HandoverId::uuid
                order by i.order_id, i.order_item_id
                """;
        }

        IEnumerable<HandoverItemDto> items = await dbQuery.QueryAsync<HandoverItemDto>(sql, new { HandoverId = handoverId }, cancellationToken);
        return [.. items];
    }

    public async Task<List<HandoverEvidenceDto>> GetHandoverEvidencesAsync(string handoverId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                he.file_url as "FileUrl",
                he.file_name as "FileName"
            from public.handovers h
            join public.handover_evidences he on h.id = he.handover_id
            where h.id = @HandoverId::uuid
            """;

        IEnumerable<HandoverEvidenceDto> evidences = await dbQuery.QueryAsync<HandoverEvidenceDto>(sql, new { HandoverId = handoverId }, cancellationToken);
        return [.. evidences];
    }

    public async Task<List<HandoverParticipantDto>> GetHandoverParticipantsAsync(string handoverId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                hp.employee_code as "EmployeeCode",
                hp.employee_name as "EmployeeName",
                hp.participation_rate as "ParticipationRate"
            from public.handovers h
            join public.handover_participants hp on h.id = hp.handover_id
            where h.id = @HandoverId::uuid
            """;

        IEnumerable<HandoverParticipantDto> participants = await dbQuery.QueryAsync<HandoverParticipantDto>(sql, new { HandoverId = handoverId }, cancellationToken);
        return [.. participants];
    }
}
