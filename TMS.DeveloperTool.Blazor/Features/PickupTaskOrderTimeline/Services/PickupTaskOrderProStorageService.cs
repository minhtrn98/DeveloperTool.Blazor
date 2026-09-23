using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskOrderTimeline.Services;

/// <summary>
/// Read-only access to <c>pro.pickup_task_orders</c> / <c>pro.pickup_task_order_items</c>
/// (filled by <c>PickupTaskOrderSyncJob</c>).
/// </summary>
public sealed class PickupTaskOrderProStorageService(IDbContextFactory<ProApplicationDbContext> dbContextFactory)
{
    public async Task<List<PickupTaskOrderItem>> GetItemsByOrderItemIdAsync(string orderItemId, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.PickupTaskOrderItems
            .AsNoTracking()
            .Where(x => x.OrderItemId == orderItemId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PickupTaskOrder>> GetOrdersByOrderIdAsync(string orderId, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.PickupTaskOrders
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PickupTaskOrderItem>> GetItemsByOrderIdAsync(string orderId, CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.PickupTaskOrderItems
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
