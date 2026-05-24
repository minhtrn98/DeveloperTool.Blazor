using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class OrderRepository([FromKeyedServices("OrderDb")] ApplicationDbQuery dbQuery)
{
    public async Task<List<PickupTaskOrderDto>> GetOrdersByPickupTaskIdAsync(string pickupTaskId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                o.pickup_task_id as "PickupTaskId",
                o.order_id as "OrderId",
                o.status as "Status"
            from public.pickup_task_orders o
            where o.pickup_task_id = @PickupTaskId
            order by o.order_id;
            """;
        IEnumerable<PickupTaskOrderDto> orders = await dbQuery.QueryAsync<PickupTaskOrderDto>(sql, new { PickupTaskId = pickupTaskId }, cancellationToken);
        return [.. orders];
    }

    public async Task<List<OrderItemDto>> GetAllOrderItemsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            with pickup_order_items as (
                select order_item_id from public.pickup_task_order_items
            )
            select
                o.order_id as "OrderId",
                o.order_item_id as "OrderItemId",
                o.weight as "Weight",
                o.w as "W",
                o.h as "H",
                o.l as "L",
                (po.order_item_id is not null) as "HasPickupTask"
            from public.order_items o
            left join pickup_order_items po on po.order_item_id = o.order_item_id
            order by o.order_item_id;
            """;
        IEnumerable<OrderItemDto> orderItems = await dbQuery.QueryAsync<OrderItemDto>(sql, null, cancellationToken);
        return [.. orderItems];
    }

    public async Task<List<PickupTaskOrderItemDto>> GetOrderItemsByPickupTaskIdAsync(string pickupTaskId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                ptoi.order_id as "OrderId",
                ptoi.order_item_id as "OrderItemId",
                ptoi.weight as "Weight",
                ptoi.w as "W",
                ptoi.h as "H",
                ptoi.l as "L",
                ptoi.status as "Status",
                true as "HasPickupTask"
            from public.pickup_task_order_items ptoi
            where ptoi.pickup_task_id = @PickupTaskId
            order by ptoi.order_id, ptoi.order_item_id;
            """;
        IEnumerable<PickupTaskOrderItemDto> orderItems = await dbQuery.QueryAsync<PickupTaskOrderItemDto>(sql, new { PickupTaskId = pickupTaskId }, cancellationToken);
        return [.. orderItems];
    }

    public async Task<List<PickupTaskDto>> GetAllPickupTasksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                p.pickup_task_id::text as "PickupTaskId",
                p.assigned_driver_id::text as "AssignedDriverId",
                p.assigned_driver_code as "AssignedDriverCode",
                p.assigned_driver_name as "AssignedDriverName",
                p.status::text as "Status",
                p.scheduled_pickup_date as "ScheduledPickupDate",
                pai.driver_pickup_priority as "PickupPriority"
            from public.pickup_tasks p
            join public.pickup_task_assigned_info pai on pai.pickup_task_id = p.pickup_task_id and pai.is_active = true
            order by p.assigned_driver_code nulls last,
                p.status desc,
                pai.driver_pickup_priority nulls last,
                p.scheduled_pickup_date nulls last,
                p.dispatched_at nulls last,
                p.pickup_task_id;
            """;
        IEnumerable<PickupTaskDto> pickupTasks = await dbQuery.QueryAsync<PickupTaskDto>(sql, null, cancellationToken);
        return [.. pickupTasks];
    }

    public async Task<List<Guid>> GetPickupTaskAssignedDriverIdsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select distinct assigned_driver_id
            from public.pickup_tasks
            where assigned_driver_id is not null
            order by assigned_driver_id;
            """;
        IEnumerable<Guid> driverIds = await dbQuery.QueryAsync<Guid>(sql, null, cancellationToken);
        return [.. driverIds];
    }

    public async Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                o.order_id as "OrderId",
                o.accepted_time as "AcceptedTime",
                o.created_at as "CreatedAt",
                o.sender_name as "SenderName",
                o.sender_address as "SenderAddress",
                o.sender_post_office_name as "SenderPostOfficeName",
                o.receiver_name as "ReceiverName",
                o.receiver_address as "ReceiverAddress",
                o.receiver_post_office_name as "ReceiverPostOfficeName",
                o.current_post_office_name as "CurrentPostOfficeName",
                o.current_status_id as "CurrentStatusId",
                o.current_status_name as "CurrentStatusName",
                o.service_type_name as "ServiceTypeName",
                o.weight as "Weight",
                o.cod_amount as "CodAmount",
                o.order_type as "OrderType"
            from public.orders o
            where o.is_deleted = false
            order by o.created_at desc
            """;

        IEnumerable<Order> orders = await dbQuery.QueryAsync<Order>(sql, null, cancellationToken);
        return [.. orders];
    }

    public async Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(
        string orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                oi.order_item_id as "OrderItemId",
                oi.order_id as "OrderId",
                oi.weight as "Weight",
                oi.real_weight as "RealWeight",
                oi.cal_weight as "CalWeight",
                oi.l as "L",
                oi.h as "H",
                oi.w as "W",
                oi.current_post_office_name as "CurrentPostOfficeName",
                oi.current_status_id as "CurrentStatusId",
                oi.current_status_name as "CurrentStatusName"
            from public.order_items oi
            where oi.order_id = @OrderId
            order by oi.order_item_id
            """;

        IEnumerable<OrderItem> items = await dbQuery.QueryAsync<OrderItem>(
            sql, new { OrderId = orderId }, cancellationToken);
        return [.. items];
    }

    public async Task<PmsOrderLookupDto?> GetOrderForPmsLookupAsync(string orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                o.order_id as "OrderId",
                o.accepted_time as "AcceptedTime",
                o.current_post_office_code as "CurrentPostOfficeCode",
                o.current_post_office_name as "CurrentPostOfficeName",
                o.receiver_post_office_code as "ReceiverPostOfficeCode",
                o.receiver_post_office_name as "ReceiverPostOfficeName",
                o.receiver_address as "ReceiverAddress",
                o.receiver_ward_id as "ReceiverWardId",
                o.receiver_ward_name as "ReceiverWardName",
                o.receiver_province_id as "ReceiverProvinceId",
                o.receiver_province_name as "ReceiverProvinceName",
                o.receiver_post_office_id as "ReceiverPostOfficeId",
                o.receiver_country_id as "ReceiverCountryId",
                o.receiver_country_name as "ReceiverCountryName",
                o.cod_amount as "CodAmount",
                o.order_type as "OrderType"
            from public.orders o
            where o.order_id = @OrderId
              and o.is_deleted = false
            """;
        return await dbQuery.SingleOrDefaultAsync<PmsOrderLookupDto>(sql, new { OrderId = orderId }, cancellationToken);
    }

    public async Task<List<PmsOrderItemLookupDto>> GetOrderItemsForPmsLookupAsync(string orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                oi.order_item_id as "OrderItemId",
                oi.order_id as "OrderId",
                oi.weight as "Weight",
                oi.cal_weight as "CalWeight",
                oi.warehouse_status as "WarehouseStatus",
                oi.destination_post_office_code as "DestinationPostOfficeCode",
                oi.destination_post_office_name as "DestinationPostOfficeName"
            from public.order_items oi
            where oi.order_id = @OrderId
            order by oi.order_item_id
            """;
        IEnumerable<PmsOrderItemLookupDto> items = await dbQuery.QueryAsync<PmsOrderItemLookupDto>(sql, new { OrderId = orderId }, cancellationToken);
        return [.. items];
    }

    public async Task<List<PackageDto>> GetAllPackagesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            with item_new as (
                select p.order_item_id, MAX(p.created_at) AS created_at
                from public.order_packages p
                GROUP BY p.order_item_id
            )
            select p1.package_code as "PackageCode", count(0) as "TotalItems"
            from public.order_packages p1
            join item_new p2 on p1.order_item_id = p2.order_item_id and p1.created_at = p2.created_at
            where p1.package_code is not null and p1.package_code != ''
            group by p1.package_code
            order by p1.package_code
            """;
        IEnumerable<PackageDto> packages = await dbQuery.QueryAsync<PackageDto>(sql, null, cancellationToken);
        return [.. packages];
    }

    public async Task<List<TripPackageDto>> GetItemsByPackageCodeAsync(string packageCode, CancellationToken cancellationToken = default)
    {
        const string sql = """
            with item_new as (
                select p.order_item_id, MAX(p.created_at) AS created_at
                from public.order_packages p
                GROUP BY p.order_item_id
            )
            select p1.order_id as "OrderId",
                   p1.order_item_id as "OrderItemId",
                   p1.order_created_at as "OrderCreatedAt",
                   p1.post_office_id as "PostOfficeId",
                   p1.status_id as "StatusId",
                   p1.package_code as "PackageCode",
                   p1.created_at as "CreatedAt",
                   p1.updated_at as "UpdatedAt"
            from public.order_packages p1
            join item_new p2 on p1.order_item_id = p2.order_item_id and p1.created_at = p2.created_at
            where p1.package_code = @PackageCode
            order by p1.order_id, p1.order_item_id
            """;
        IEnumerable<TripPackageDto> items = await dbQuery.QueryAsync<TripPackageDto>(sql, new { PackageCode = packageCode }, cancellationToken);
        return [.. items];
    }

    public async Task<List<TripDto>> GetAllTripsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            with item_new as (
                select p.order_item_id, MAX(p.created_at) AS created_at
                from public.order_packages p
                GROUP BY p.order_item_id
            )
            select p1.trip_code as "TripCode", count(0) as "TotalItems"
            from public.order_packages p1
            join item_new p2 on p1.order_item_id = p2.order_item_id and p1.created_at = p2.created_at
            where p1.trip_code is not null
            group by p1.trip_code
            order by p1.trip_code
            """;
        IEnumerable<TripDto> trips = await dbQuery.QueryAsync<TripDto>(sql, null, cancellationToken);
        return [.. trips];
    }

    public async Task<List<TripPackageDto>> GetPackagesByTripCodeAsync(string tripCode, CancellationToken cancellationToken = default)
    {
        const string sql = """
            with item_new as (
                select p.order_item_id, MAX(p.created_at) AS created_at
                from public.order_packages p
                GROUP BY p.order_item_id
            )
            select p1.order_id as "OrderId",
                   p1.order_item_id as "OrderItemId",
                   p1.order_created_at as "OrderCreatedAt",
                   p1.post_office_id as "PostOfficeId",
                   p1.status_id as "StatusId",
                   p1.package_code as "PackageCode",
                   p1.created_at as "CreatedAt",
                   p1.updated_at as "UpdatedAt"
            from public.order_packages p1
            join item_new p2 on p1.order_item_id = p2.order_item_id and p1.created_at = p2.created_at
            where p1.trip_code = @TripCode
            order by p1.order_id, p1.order_item_id
            """;
        IEnumerable<TripPackageDto> items = await dbQuery.QueryAsync<TripPackageDto>(sql, new { TripCode = tripCode }, cancellationToken);
        return [.. items];
    }

    public async Task<List<PickupTaskOrderDraftDto>> GetPickupTaskOrderDraftsByPickupTaskIdAsync(string pickupTaskId, CancellationToken cancellationToken = default)
    {
        const string sqlItem = """
            select
                pickup_task_id as "PickupTaskId",
                draft_id as "DraftId",
                draft_item_id as "DraftItemId"
            from public.pickup_task_order_item_drafts
            where pickup_task_id = @PickupTaskId
            order by draft_item_id;
            """;

        IEnumerable<PickupTaskOrderDraftItemDto> items = await dbQuery.QueryAsync<PickupTaskOrderDraftItemDto>(sqlItem, new { PickupTaskId = pickupTaskId }, cancellationToken);

        List<string> draftIds = [.. items.Select(i => i.DraftId).Distinct()];

        const string sqlDraft = """
            select
                draft_id as "DraftId"
            from public.pickup_task_order_drafts
            join UNNEST(@DraftIds) as d(id) on draft_id = d.id
            order by draft_id;
            """;
        IEnumerable<PickupTaskOrderDraftDto> drafts = await dbQuery.QueryAsync<PickupTaskOrderDraftDto>(sqlDraft, new { DraftIds = draftIds }, cancellationToken);
        foreach (var draft in drafts)
        {
            draft.Items = [.. items.Where(i => i.DraftId == draft.DraftId)];
        }

        return [.. drafts];
    }
}
