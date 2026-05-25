using Microsoft.AspNetCore.Mvc;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Features.Item.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.Item;

[ApiController]
[Route("api/[controller]")]
public class ItemController(OrderRepository orderRepository) : ControllerBase
{
    [HttpPost("Lookup")]
    public async Task<ActionResult<PmsScanLookupResponse>> LookupAsync(
        [FromBody] PmsScanLookupRequest request,
        CancellationToken cancellationToken)
    {
        PmsOrderLookupDto? order = await orderRepository.GetOrderForPmsLookupAsync(request.Id, cancellationToken);
        if (order is null)
            return NotFound();

        List<PmsOrderItemLookupDto> items = await orderRepository.GetOrderItemsForPmsLookupAsync(request.Id, cancellationToken);

        return Ok(MapToResponse(request.Id, order, items));
    }

    private static PmsScanLookupResponse MapToResponse(
        string scannedCode,
        PmsOrderLookupDto order,
        List<PmsOrderItemLookupDto> items)
    {
        return new PmsScanLookupResponse
        {
            ScannedCode = scannedCode,
            ScannedCodeType = PmsScannedCodeType.OrderItem,
            OriginPostOfficeCode = order.CurrentPostOfficeCode,
            OriginPostOfficeName = order.CurrentPostOfficeName,
            DestinationPostOfficeCode = order.ReceiverPostOfficeCode,
            DestinationPostOfficeName = order.ReceiverPostOfficeName,
            IssuedAt = order.AcceptedTime?.UtcDateTime,
            Items = items.Select(oi => new PmsScanItem
            {
                ItemId = oi.OrderItemId,
                ItemLevel = 1,
                OrderId = oi.OrderId,
                Weight = oi.Weight,
                CalWeight = oi.CalWeight,
                WarehouseStatus = oi.WarehouseStatus,
                DestinationPostOfficeCode = oi.DestinationPostOfficeCode,
                DestinationPostOfficeName = oi.DestinationPostOfficeName,
                OrderType = order.OrderType,
                ReceiverAddress = order.ReceiverAddress,
                ReceiverWardId = order.ReceiverWardId,
                ReceiverWardName = order.ReceiverWardName,
                ReceiverProvinceId = order.ReceiverProvinceId,
                ReceiverProvinceName = order.ReceiverProvinceName,
                ReceiverPostOfficeId = order.ReceiverPostOfficeId,
                ReceiverPostOfficeName = order.ReceiverPostOfficeName,
                ReceiverCountryId = order.ReceiverCountryId,
                ReceiverCountryName = order.ReceiverCountryName,
                CodAmount = order.CodAmount,
                CurrentPostOfficeId = oi.CurrentPostOfficeId,
                CurrentPostOfficeName = oi.CurrentPostOfficeName
            }).ToList()
        };
    }
}
