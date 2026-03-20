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

        _mappings = new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "OrderId",
                new JsonKeyMapping(
                    orderIds,
                    [
                        new DependentValueMapping("CreatedAt", orderIdToCreatedAtMap)
                    ])
            },
            {
                "pickupPostOfficeCode",
                new JsonKeyMapping(
                    postOfficeCodes,
                    [
                        new DependentValueMapping("pickupPostOfficeName", postOfficeCodeToNameMap)
                    ])
            }
        };
        return _mappings;
    }

    private static string FormatCreatedAt(DateTime value)
    {
        return value.ToLocalTime().ToString("O");
    }
}