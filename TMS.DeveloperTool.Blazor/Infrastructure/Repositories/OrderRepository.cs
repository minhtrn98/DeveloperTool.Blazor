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
                o.created_at as "CreatedAt",
                (po.order_id is not null) as "HasPickupTask"
            from public.orders o
            left join pickup_orders po on po.order_id = o.order_id
            order by o.order_id;
            """;
        IEnumerable<OrderInfo> orders = await _dbQuery.QueryAsync<OrderInfo>(sql, null, cancellationToken);
        return orders.ToList();
    }
}
