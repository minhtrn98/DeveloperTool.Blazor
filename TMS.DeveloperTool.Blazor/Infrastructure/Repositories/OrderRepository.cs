namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class OrderRepository([FromKeyedServices("OrderDb")] ApplicationDbQuery dbQuery)
{
    public async Task<PmsOrderLookupDto?> GetOrderForPmsLookupAsync(string orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                o.order_id as "OrderId",
                o.accepted_time as "AcceptedTime",
                o.current_post_office_id as "CurrentPostOfficeCode",
                o.current_post_office_name as "CurrentPostOfficeName",
                o.receiver_post_office_id as "ReceiverPostOfficeCode",
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
                '' as "DestinationPostOfficeCode",
                '' as "DestinationPostOfficeName",
                oi.current_post_office_id as "CurrentPostOfficeId",
                oi.current_post_office_name as "CurrentPostOfficeName"
            from public.order_items oi
            where oi.order_id = @OrderId
            order by oi.order_item_id
            """;
        IEnumerable<PmsOrderItemLookupDto> items = await dbQuery.QueryAsync<PmsOrderItemLookupDto>(sql, new { OrderId = orderId }, cancellationToken);
        return [.. items];
    }
}
