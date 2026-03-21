using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Extensions;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.Routing.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public sealed class PickupTaskEventJsonMappingStrategy(
    OrderRepository orderRepository,
    RouteRepository routeRepository) : JsonTypeMappingStrategyBase
{
    public const string TypeName = "RabbitMqPickupTaskEvent";
    public override string JsonType => TypeName;

    private readonly Lazy<Task<Dictionary<string, JsonKeyMapping>>> _mappingsTask =
        new(() => BuildMappingsInternal(orderRepository, routeRepository), LazyThreadSafetyMode.ExecutionAndPublication);

    public override async Task<Dictionary<string, JsonKeyMapping>> BuildMappings()
    {
        return await _mappingsTask.Value;
    }

    private static async Task<Dictionary<string, JsonKeyMapping>> BuildMappingsInternal(
        OrderRepository orderRepository,
        RouteRepository routeRepository)
    {
        List<OrderInfo> orders = await orderRepository.GetAllOrdersAsync();
        IEnumerable<PostOffice> postOffices = await routeRepository.GetPostOfficesAsync();

        return new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrderId"] = CreateOrderIdMapping(orders),
            ["pickupPostOfficeCode"] = CreatePickupPostOfficeCodeMapping(postOffices),
            ["statusId"] = CreateStatusIdMapping(),
            ["dispatchType"] = CreateDispatchTypeMapping(),
            ["dispatchMethod"] = CreateDispatchMethodMapping()
        };
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
        Dictionary<string, string> postOfficeCodeToPickupTaskIdPrefixMap = postOffices
            .Where(p => !string.IsNullOrWhiteSpace(p.PostOfficeCode))
            .Select(p => p.PostOfficeCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                code => code,
                code => code,
                StringComparer.OrdinalIgnoreCase);

        return new JsonKeyMapping(
            [.. postOfficeCodes],
            [
                new DependentValueMapping("pickupPostOfficeName", ToObjectMap(postOfficeCodeToNameMap)),
                new DependentValueMapping("pickupTaskId", ToObjectMap(postOfficeCodeToPickupTaskIdPrefixMap), BuildPickupTaskId)
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

    private static object? BuildPickupTaskId(DependentValueContext context)
    {
        string newPrefix = context.NewValue?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newPrefix))
        {
            return context.OldValue;
        }

        string currentPickupTaskId = context.OldValue?.ToString() ?? string.Empty;
        int firstDashIndex = currentPickupTaskId.IndexOf('-');
        if (firstDashIndex < 0 || firstDashIndex >= currentPickupTaskId.Length - 1)
        {
            return newPrefix;
        }

        string currentSuffix = currentPickupTaskId[(firstDashIndex + 1)..];
        return $"{newPrefix}-{currentSuffix}";
    }

    private static Dictionary<object, object> ToObjectMap(Dictionary<string, string> source)
    {
        return source.ToDictionary(kvp => (object)kvp.Key, kvp => (object)kvp.Value);
    }

    private static string FormatCreatedAt(DateTime value)
    {
        return value.ToLocalTime().ToString("O");
    }
}