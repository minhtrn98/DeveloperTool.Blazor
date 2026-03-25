using System.Collections.ObjectModel;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.Routing.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;

public sealed class PickupTaskEventJsonMappingStrategy(
        OrderRepository orderRepository,
        RouteRepository routeRepository,
        EventService eventService
    ) : JsonTypeMappingStrategyBase
{
    public const string TypeName = "RabbitMqPickupTaskEvent";
    public override string JsonType => TypeName;

    private readonly IReadOnlyDictionary<string, JsonKeyValueBuilder> _keyValueBuilders =
        new Dictionary<string, JsonKeyValueBuilder>(StringComparer.OrdinalIgnoreCase)
        {
            ["orders"] = async () => await BuildOrdersValueAsync(orderRepository)
        };

    private readonly Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>> _jsonKeyMappingsTask =
        new(() => BuildMappingsAsync(routeRepository), LazyThreadSafetyMode.ExecutionAndPublication);

    public override IReadOnlyDictionary<string, JsonKeyValueBuilder> KeyValueBuilders => _keyValueBuilders;

    public override async Task<IReadOnlyDictionary<string, JsonKeyMapping>> BuildMappingsAsync()
    {
        return await _jsonKeyMappingsTask.Value;
    }

    public override async Task SendRequestAsync(string jsonInput, CancellationToken cancellationToken = default)
    {
        await eventService.PublishPickupTaskEvent(jsonInput, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, JsonKeyMapping>> BuildMappingsAsync(
        RouteRepository routeRepository)
    {
        IEnumerable<PostOffice> postOffices = await routeRepository.GetPostOfficesAsync();

        Dictionary<string, JsonKeyMapping> mappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pickupPostOfficeCode"] = CreatePickupPostOfficeCodeMapping(postOffices),
            ["statusId"] = CreateStatusIdMapping(),
            ["serviceTypeId"] = CreateServiceTypeIdMapping(),
            ["dispatchType"] = CreateDispatchTypeMapping(),
            ["dispatchMethod"] = CreateDispatchMethodMapping()
        };

        return new ReadOnlyDictionary<string, JsonKeyMapping>(mappings);
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

    private static JsonKeyMapping CreateServiceTypeIdMapping()
    {
        Dictionary<object, object> serviceTypeIdToNameMap = ToObjectMap(EnumExtensions.ToValueDescriptionMap<TransportServiceType>());

        return new JsonKeyMapping(
            [.. serviceTypeIdToNameMap.Keys],
            [
                new DependentValueMapping("serviceTypeName", serviceTypeIdToNameMap)
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

    private static async Task<object> BuildOrdersValueAsync(OrderRepository orderRepository)
    {
        List<OrderInfo> orders = await orderRepository.GetAllOrdersAsync();
        List<OrderItemInfo> orderItems = await orderRepository.GetAllOrderItemsAsync();

        Dictionary<string, List<PickupTaskEventOrderItemDto>> orderItemsByOrderId = orderItems
            .Where(orderItem => !string.IsNullOrWhiteSpace(orderItem.OrderId))
            .GroupBy(orderItem => orderItem.OrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(orderItem => new PickupTaskEventOrderItemDto(
                        orderItem.OrderId,
                        orderItem.OrderItemId,
                        orderItem.Weight,
                        orderItem.L,
                        orderItem.H,
                        orderItem.W,
                        orderItem.HasPickupTask))
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        List<PickupTaskEventOrderDto> orderDtos = orders
            .Where(order => !string.IsNullOrWhiteSpace(order.OrderId))
            .Select(order => new PickupTaskEventOrderDto(
                order.OrderId,
                FormatCreatedAt(order.CreatedAt),
                order.Weight,
                order.L,
                order.H,
                order.W,
                order.HasPickupTask,
                orderItemsByOrderId.GetValueOrDefault(order.OrderId, [])))
            .ToList();

        return orderDtos;
    }

    private static string FormatCreatedAt(DateTime value)
    {
        return value.ToLocalTime().ToString("O");
    }
}