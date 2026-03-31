namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class OrderRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public OrderRepository([FromKeyedServices("OrderDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<List<OrderInfo>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
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
        IEnumerable<OrderInfo> orders = await _dbQuery.QueryAsync<OrderInfo>(sql, null, cancellationToken);
        return orders.ToList();
    }

    public async Task<List<PickupTaskOrderInfo>> GetOrdersByPickupTaskIdAsync(string pickupTaskId, CancellationToken cancellationToken = default)
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
        IEnumerable<PickupTaskOrderInfo> orders = await _dbQuery.QueryAsync<PickupTaskOrderInfo>(sql, new { PickupTaskId = pickupTaskId }, cancellationToken);
        return orders.ToList();
    }

    public async Task<List<OrderItemInfo>> GetAllOrderItemsAsync(CancellationToken cancellationToken = default)
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
                o.status as "Status",
                (po.order_item_id is not null) as "HasPickupTask"
            from public.order_items o
            left join pickup_order_items po on po.order_item_id = o.order_item_id
            order by o.order_item_id;
            """;
        IEnumerable<OrderItemInfo> orderItems = await _dbQuery.QueryAsync<OrderItemInfo>(sql, null, cancellationToken);
        return orderItems.ToList();
    }

    public async Task<List<OrderItemInfo>> GetOrderItemsByPickupTaskIdAsync(string pickupTaskId, CancellationToken cancellationToken = default)
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
        IEnumerable<OrderItemInfo> orderItems = await _dbQuery.QueryAsync<OrderItemInfo>(sql, new { PickupTaskId = pickupTaskId }, cancellationToken);
        return orderItems.ToList();
    }

    public async Task<List<PickupTaskInfo>> GetAllPickupTaskAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                p.pickup_task_id::text as "PickupTaskId",
                p.assigned_driver_id::text as "AssignedDriverId",
                p.assigned_driver_code as "AssignedDriverCode",
                p.assigned_driver_name as "AssignedDriverName",
                p.status::text as "Status",
                p.scheduled_pickup_date as "ScheduledPickupDate"
            from public.pickup_tasks p
            order by p.scheduled_pickup_date desc nulls last, p.pickup_task_id;
            """;
        IEnumerable<PickupTaskInfo> pickupTasks = await _dbQuery.QueryAsync<PickupTaskInfo>(sql, null, cancellationToken);
        return pickupTasks.ToList();
    }
}
