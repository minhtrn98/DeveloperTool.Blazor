using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.Routing.Models;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Extensions;

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
        IEnumerable<PostOffice> postOffices = await routeRepository.GetPostOfficesAsync();

        _mappings = new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrderId"] = CreateOrderIdMapping(orders),
            ["pickupPostOfficeCode"] = CreatePickupPostOfficeCodeMapping(postOffices),
            ["statusId"] = CreateStatusIdMapping(),
            ["dispatchType"] = CreateDispatchTypeMapping(),
            ["dispatchMethod"] = CreateDispatchMethodMapping()
        };
        return _mappings;
    }

    private static string FormatCreatedAt(DateTime value)
    {
        return value.ToLocalTime().ToString("O");
    }

    private static JsonKeyMapping CreateOrderIdMapping(IEnumerable<OrderInfo> orders)
    {
        List<object> orderIds = orders
            .Select(o => o.OrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .Cast<object>()
            .ToList();
        Dictionary<string, string> orderIdToCreatedAtMap = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.OrderId))
            .ToDictionary(
                o => o.OrderId,
                o => FormatCreatedAt(o.CreatedAt),
                StringComparer.OrdinalIgnoreCase);

        return new JsonKeyMapping(
            [.. orderIds],
            [
                new DependentValueMapping("CreatedAt", ToObjectMap(orderIdToCreatedAtMap))
            ]);
    }

    private static JsonKeyMapping CreatePickupPostOfficeCodeMapping(IEnumerable<PostOffice> postOffices)
    {
        List<object> postOfficeCodes = postOffices
            .Select(p => p.PostOfficeCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .Cast<object>()
            .ToList();
        Dictionary<string, string> postOfficeCodeToNameMap = postOffices
            .Where(p => !string.IsNullOrWhiteSpace(p.PostOfficeCode) && !string.IsNullOrWhiteSpace(p.PostOfficeName))
            .GroupBy(p => p.PostOfficeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().PostOfficeName,
                StringComparer.OrdinalIgnoreCase);

        return new JsonKeyMapping(
            [.. postOfficeCodes],
            [
                new DependentValueMapping("pickupPostOfficeName", ToObjectMap(postOfficeCodeToNameMap))
            ]);
    }

    private static JsonKeyMapping CreateStatusIdMapping()
    {
        Dictionary<object, object> statusIdToNameMap = ToObjectMap(EnumExtensions.ToValueDescriptionMap<PickupTaskStatusId>());

        return new JsonKeyMapping(
            [.. statusIdToNameMap.Keys],
            [
                new DependentValueMapping("statusName", statusIdToNameMap)
            ]);
    }

    private static JsonKeyMapping CreateDispatchTypeMapping()
    {
        Dictionary<object, object> dispatchTypeDescriptionToValueMap = EnumExtensions.ToList<DispatchType>()
            .ToDictionary(x => (object)x.Description, x => (object)x.Value);

        return new JsonKeyMapping(
            [.. dispatchTypeDescriptionToValueMap.Keys],
            [
                new DependentValueMapping("dispatchType", dispatchTypeDescriptionToValueMap)
            ]);
    }

    private static JsonKeyMapping CreateDispatchMethodMapping()
    {
        Dictionary<object, object> dispatchMethodDescriptionToValueMap = EnumExtensions.ToList<DispatchMethod>()
            .ToDictionary(x => (object)x.Description, x => (object)x.Value);

        return new JsonKeyMapping(
            [.. dispatchMethodDescriptionToValueMap.Keys],
            [
                new DependentValueMapping("dispatchMethod", dispatchMethodDescriptionToValueMap)
            ]);
    }

    private static Dictionary<object, object> ToObjectMap(Dictionary<string, string> source)
    {
        return source.ToDictionary(kvp => (object)kvp.Key, kvp => (object)kvp.Value);
    }
}