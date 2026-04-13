namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class OrderRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public OrderRepository([FromKeyedServices("OrderDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            with pickup_orders as (
                select order_id from public.pickup_task_order_items
                union
                select order_id from public.pickup_task_orders
            )
            select
                o.order_id as "OrderId",
                o.weight as "Weight",
                o.w as "W",
                o.h as "H",
                o.l as "L",
                o.created_at as "CreatedAt",
                (po.order_id is not null) as "HasPickupTask"
            from public.orders o
            left join pickup_orders po on po.order_id = o.order_id
            order by o.order_id;
            """;
        IEnumerable<OrderDto> orders = await _dbQuery.QueryAsync<OrderDto>(sql, null, cancellationToken);
        return orders.ToList();
    }

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
        IEnumerable<PickupTaskOrderDto> orders = await _dbQuery.QueryAsync<PickupTaskOrderDto>(sql, new { PickupTaskId = pickupTaskId }, cancellationToken);
        return orders.ToList();
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
        IEnumerable<OrderItemDto> orderItems = await _dbQuery.QueryAsync<OrderItemDto>(sql, null, cancellationToken);
        return orderItems.ToList();
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
        IEnumerable<PickupTaskOrderItemDto> orderItems = await _dbQuery.QueryAsync<PickupTaskOrderItemDto>(sql, new { PickupTaskId = pickupTaskId }, cancellationToken);
        return orderItems.ToList();
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
                pai.driver_pickup_priority nulls last,
                p.scheduled_pickup_date desc nulls last,
                p.dispatched_at desc nulls last,
                p.pickup_task_id;
            """;
        IEnumerable<PickupTaskDto> pickupTasks = await _dbQuery.QueryAsync<PickupTaskDto>(sql, null, cancellationToken);
        return pickupTasks.ToList();
    }

    public async Task<List<Guid>> GetPickupTaskAssignedDriverIdsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select distinct assigned_driver_id
            from public.pickup_tasks
            where assigned_driver_id is not null
            order by assigned_driver_id;
            """;
        IEnumerable<Guid> driverIds = await _dbQuery.QueryAsync<Guid>(sql, null, cancellationToken);
        return driverIds.ToList();
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

        IEnumerable<PickupTaskOrderDraftItemDto> items = await _dbQuery.QueryAsync<PickupTaskOrderDraftItemDto>(sqlItem, new { PickupTaskId = pickupTaskId }, cancellationToken);

        List<string> draftIds = items.Select(i => i.DraftId).Distinct().ToList();

        const string sqlDraft = """
            select
                draft_id as "DraftId"
            from public.pickup_task_order_drafts
            join UNNEST(@DraftIds) as d(id) on draft_id = d.id
            order by draft_id;
            """;
        IEnumerable<PickupTaskOrderDraftDto> drafts = await _dbQuery.QueryAsync<PickupTaskOrderDraftDto>(sqlDraft, new { DraftIds = draftIds }, cancellationToken);
        foreach (var draft in drafts)
        {
            draft.Items = items.Where(i => i.DraftId == draft.DraftId).ToList();
        }

        return drafts.ToList();
    }
}
