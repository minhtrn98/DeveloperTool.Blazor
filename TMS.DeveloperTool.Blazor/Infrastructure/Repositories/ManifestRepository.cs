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
}
