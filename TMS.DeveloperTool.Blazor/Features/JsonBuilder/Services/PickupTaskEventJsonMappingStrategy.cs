using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.Routing.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public sealed class PickupTaskEventJsonMappingStrategy(
    OrderRepository orderRepository,
    RouteRepository routeRepository) : IJsonTypeMappingStrategy
{
    public const string TypeName = "RabbitMqPickupTaskEvent";
    public string JsonType => TypeName;

    private Dictionary<string, JsonKeyMapping> _mappings = [];

    public async Task<Dictionary<string, JsonKeyMapping>> BuildMappings()
    {
        if (_mappings.Count > 0)
        {
            return _mappings;
        }

        List<OrderInfo> orders = await orderRepository.GetAllOrdersAsync();
        List<string> orderIds = orders
            .Select(o => o.OrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        Dictionary<string, string> orderIdToCreatedAtMap = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.OrderId))
            .ToDictionary(
                o => o.OrderId,
                o => FormatCreatedAt(o.CreatedAt),
                StringComparer.OrdinalIgnoreCase);

        IEnumerable<PostOffice> postOffices = await routeRepository.GetPostOfficesAsync();
        List<string> postOfficeCodes = postOffices
            .Select(p => p.PostOfficeCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        Dictionary<string, string> postOfficeCodeToNameMap = postOffices
            .Where(p => !string.IsNullOrWhiteSpace(p.PostOfficeCode) && !string.IsNullOrWhiteSpace(p.PostOfficeName))
            .GroupBy(p => p.PostOfficeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().PostOfficeName,
                StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> statusIdToNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["waiting_for_assignment"] = "Chờ phân công",
            ["waiting_for_acceptance"] = "Chờ tiếp nhận",
            ["in_transit"] = "Đang di chuyển",
            ["arrived"] = "Đã đến",
            ["picking_up"] = "Đang lấy hàng",
            ["creating_order"] = "Đang tạo đơn",
            ["completed"] = "Lấy thành công",
            ["pickup_failed"] = "Chưa lấy được",
            ["cancelled"] = "Hủy lấy đơn",
            ["transferred_to_post_office"] = "Chuyển bưu cục",
            ["waiting_for_vehicle_rental"] = "Chờ thuê xe",
            ["vehicle_rental_in_progress"] = "Đang thuê xe"
        };
        List<string> statusIds = statusIdToNameMap.Keys.ToList();
        Dictionary<object, object> dispatchTypeDescriptionToValueMap = new()
        {
            ["Đi nhận - KH bưu cục"] = 1,
            ["Đi nhận - KH hệ thống"] = 2,
            ["Đi nhận - nhận hộ"] = 3,
            ["Đi nhận - khách lẻ"] = 4,
            ["Đi nhận - KH Web/API"] = 5,
            ["Gửi hàng"] = 6,
            ["Đón hàng"] = 7,
            ["Cứu hộ hàng"] = 8,
            ["Kết nối"] = 9,
            ["Nối chuyến"] = 10
        };
        List<object> dispatchTypeDescriptions = [.. dispatchTypeDescriptionToValueMap.Keys];
        Dictionary<object, object> dispatchMethodDescriptionToValueMap = new()
        {
            ["Điều nhận"] = 1,
            ["Điều chở"] = 2
        };
        List<object> dispatchMethodDescriptions = [.. dispatchMethodDescriptionToValueMap.Keys];

        _mappings = new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "OrderId",
                new JsonKeyMapping(
                    [.. orderIds.Cast<object>()],
                    [
                        new DependentValueMapping("CreatedAt", ToObjectMap(orderIdToCreatedAtMap))
                    ])
            },
            {
                "pickupPostOfficeCode",
                new JsonKeyMapping(
                    [.. postOfficeCodes.Cast<object>()],
                    [
                        new DependentValueMapping("pickupPostOfficeName", ToObjectMap(postOfficeCodeToNameMap))
                    ])
            },
            {
                "statusId",
                new JsonKeyMapping(
                    [.. statusIds.Cast<object>()],
                    [
                        new DependentValueMapping("statusName", ToObjectMap(statusIdToNameMap))
                    ])
            },
            {
                "dispatchType",
                new JsonKeyMapping(
                    dispatchTypeDescriptions,
                    [
                        new DependentValueMapping("dispatchType", dispatchTypeDescriptionToValueMap)
                    ])
            },
            {
                "dispatchMethod",
                new JsonKeyMapping(
                    dispatchMethodDescriptions,
                    [
                        new DependentValueMapping("dispatchMethod", dispatchMethodDescriptionToValueMap)
                    ])
            }
        };
        return _mappings;
    }

    private static string FormatCreatedAt(DateTime value)
    {
        return value.ToLocalTime().ToString("O");
    }

    private static Dictionary<object, object> ToObjectMap(Dictionary<string, string> source)
    {
        return source.ToDictionary(kvp => (object)kvp.Key, kvp => (object)kvp.Value);
    }
}