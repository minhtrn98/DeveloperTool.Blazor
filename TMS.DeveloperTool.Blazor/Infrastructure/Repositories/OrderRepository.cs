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
                (po.order_item_id is not null) as "HasPickupTask"
            from public.order_items o
            left join pickup_order_items po on po.order_item_id = o.order_item_id
            order by o.order_item_id;
            """;
        IEnumerable<OrderItemInfo> orderItems = await _dbQuery.QueryAsync<OrderItemInfo>(sql, null, cancellationToken);
        return orderItems.ToList();
    }
}
