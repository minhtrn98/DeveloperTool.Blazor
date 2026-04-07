using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.Routing.Models;

namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Services;

public sealed class CreatePickupTaskEventService(
    OrderRepository orderRepository,
    RouteRepository routeRepository,
    PlanningRepository planningRepository,
    EventService eventService,
    IWebHostEnvironment webHostEnvironment)
{
    private const string RandomChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Lock PickupTaskIdLock = new();
    private static readonly HashSet<string> GeneratedPickupTaskIds = [];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<List<DailyPlanDto>> GetDailyPlansAsync(string? status)
    {
        IEnumerable<DailyPlanDto> plans = await planningRepository.GetDailyPlans(status, CancellationToken.None);
        return plans.ToList();
    }

    public async Task<List<PostOfficeOption>> GetPostOfficeOptionsAsync()
    {
        IEnumerable<PostOffice> postOffices = await routeRepository.GetPostOfficesAsync();

        return postOffices
            .Where(p => !string.IsNullOrWhiteSpace(p.PostOfficeCode))
            .GroupBy(p => p.PostOfficeCode, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                PostOffice first = g.First();
                return new PostOfficeOption(first.PostOfficeCode, first.PostOfficeName);
            })
            .OrderBy(x => x.Code)
            .ToList();
    }

    public static List<EnumOption> GetStatusOptions()
    {
        return EnumExtensions.ToValueDescriptionMap<PickupTaskStatusId>()
            .Select(kvp => new EnumOption(kvp.Key, kvp.Value))
            .ToList();
    }

    public static List<EnumOption> GetServiceTypeOptions()
    {
        return EnumExtensions.ToValueDescriptionMap<TransportServiceType>()
            .Select(kvp => new EnumOption(kvp.Key, kvp.Value))
            .ToList();
    }

    public static List<EnumOption> GetDispatchTypeOptions()
    {
        return EnumExtensions.ToList<DispatchType>()
            .Select(x => new EnumOption(x.Value.ToString(), x.Description))
            .ToList();
    }

    public static List<EnumOption> GetDispatchMethodOptions()
    {
        return EnumExtensions.ToList<DispatchMethod>()
            .Select(x => new EnumOption(x.Value.ToString(), x.Description))
            .ToList();
    }

    public async Task<List<PickupTaskEventOrderDto>> GetOrdersAsync()
    {
        List<OrderInfo> orders = await orderRepository.GetAllOrdersAsync();
        List<OrderItemInfo> orderItems = await orderRepository.GetAllOrderItemsAsync();

        Dictionary<string, List<PickupTaskEventOrderItemDto>> orderItemsByOrderId = orderItems
            .Where(oi => !string.IsNullOrWhiteSpace(oi.OrderId))
            .GroupBy(oi => oi.OrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(oi => new PickupTaskEventOrderItemDto(
                    oi.OrderId, oi.OrderItemId, oi.Weight, oi.L, oi.H, oi.W, oi.HasPickupTask)).ToList(),
                StringComparer.OrdinalIgnoreCase);

        return orders
            .Where(o => !string.IsNullOrWhiteSpace(o.OrderId))
            .Select(o => new PickupTaskEventOrderDto(
                o.OrderId,
                o.CreatedAt.ToLocalTime().ToString("O"),
                o.Weight, o.L, o.H, o.W, o.HasPickupTask,
                orderItemsByOrderId.GetValueOrDefault(o.OrderId, [])))
            .ToList();
    }

    public string BuildJson(CreatePickupTaskEventInput input)
    {
        string templateContent = LoadTemplate();
        JsonNode? jsonNode = JsonNode.Parse(templateContent);
        if (jsonNode is null)
        {
            return templateContent;
        }

        // Apply post office
        if (!string.IsNullOrWhiteSpace(input.PostOfficeCode))
        {
            SetValue(jsonNode, "pickupPostOfficeCode", input.PostOfficeCode);
            SetValue(jsonNode, "pickupPostOfficeName", input.PostOfficeName ?? string.Empty);
            SetValue(jsonNode, "pickupPostOfficeId", input.PostOfficeCode);
            UpdatePickupTaskIdPrefix(jsonNode, input.PostOfficeCode);
        }

        // Apply status
        if (!string.IsNullOrWhiteSpace(input.StatusId))
        {
            SetValue(jsonNode, "statusId", input.StatusId);
            SetValue(jsonNode, "statusName", input.StatusName ?? string.Empty);
        }

        // Apply service type
        if (!string.IsNullOrWhiteSpace(input.ServiceTypeId))
        {
            SetValue(jsonNode, "serviceTypeId", input.ServiceTypeId);
            SetValue(jsonNode, "serviceTypeName", input.ServiceTypeName ?? string.Empty);
        }

        // Apply dispatch type
        if (input.DispatchType.HasValue)
        {
            SetValue(jsonNode, "dispatchType", input.DispatchType.Value);
        }

        // Apply dispatch method
        if (input.DispatchMethod.HasValue)
        {
            SetValue(jsonNode, "dispatchMethod", input.DispatchMethod.Value);
        }

        // Apply scheduled pickup date
        if (input.ScheduledPickupDate.HasValue)
        {
            DateTimeOffset dto = new(input.ScheduledPickupDate.Value, TimeSpan.FromHours(7));
            SetValue(jsonNode, "scheduledPickupDate", dto.ToString("O"));
        }

        // Apply orders
        if (input.SelectedOrders.Count > 0)
        {
            JsonArray ordersArray = JsonSerializer.SerializeToNode(input.SelectedOrders, JsonOptions)!.AsArray();
            jsonNode["orders"] = ordersArray;

            int totalItems = input.SelectedOrders.Sum(o => o.Items.Count);
            totalItems = totalItems > 0 ? totalItems : input.SelectedOrders.Count;
            decimal totalWeight = input.SelectedOrders.Sum(o => o.Weight);
            decimal totalL = input.SelectedOrders.Sum(o => o.L);
            decimal totalH = input.SelectedOrders.Sum(o => o.H);
            decimal totalW = input.SelectedOrders.Sum(o => o.W);
            decimal totalCalWeight = totalL * totalH * totalW / 6000m;

            SetValue(jsonNode, "totalItems", totalItems);
            SetValue(jsonNode, "totalWeight", totalWeight);
            SetValue(jsonNode, "totalCalWeight", totalCalWeight);
            SetValue(jsonNode, "totalCodAmount", 0);
            SetValue(jsonNode, "l", totalL);
            SetValue(jsonNode, "h", totalH);
            SetValue(jsonNode, "w", totalW);
        }

        // set created at to now
        SetValue(jsonNode, "createdAt", DateTimeOffset.Now.ToString("O"));

        return jsonNode.ToJsonString(JsonOptions);
    }

    public async Task SendAsync(string json, CancellationToken cancellationToken = default)
    {
        await eventService.PublishPickupTaskEvent(json, cancellationToken);
    }

    public List<string> BuildJsonListFromDailyPlan(
        DailyPlanDto plan,
        List<PostOfficeOption> postOfficeOptions,
        CreatePickupTaskEventInput baseInput,
        int intervalMinutes = 15)
    {
        List<DailyPlanDetailDto> pickupDetails = plan.Details
            .Where(d => d.BusinessOperation is BusinessOperation.Receive or BusinessOperation.ReceiveAndDelivery)
            .OrderBy(d => d.StepNumber)
            .ToList();

        List<string> jsonList = [];

        foreach (DailyPlanDetailDto detail in pickupDetails)
        {
            PostOfficeOption? postOffice = postOfficeOptions
                .FirstOrDefault(po => string.Equals(po.Code, detail.PostOfficeCode, StringComparison.OrdinalIgnoreCase));

            // Generate multiple events spaced by intervalMinutes within FromTime-ToTime
            int count = 0;
            TimeOnly current = detail.FromTime;
            while (current < detail.ToTime)
            {
                CreatePickupTaskEventInput input = new()
                {
                    PostOfficeCode = detail.PostOfficeCode,
                    PostOfficeName = postOffice?.Name ?? detail.PostOfficeCode,
                    StatusId = baseInput.StatusId,
                    StatusName = baseInput.StatusName,
                    ServiceTypeId = baseInput.ServiceTypeId,
                    ServiceTypeName = baseInput.ServiceTypeName,
                    DispatchType = baseInput.DispatchType,
                    DispatchMethod = baseInput.DispatchMethod,
                    ScheduledPickupDate = plan.ExecutionDate.ToDateTime(current),
                    SelectedOrders = baseInput.SelectedOrders
                };

                jsonList.Add(BuildJson(input));
                current = current.Add(TimeSpan.FromMinutes(intervalMinutes));
                if (++count > 2)
                {
                    break; // Prevent infinite loop in case of data issues
                }
            }
        }

        return jsonList;
    }

    private string LoadTemplate()
    {
        string[] candidatePaths =
        [
            Path.Combine(webHostEnvironment.ContentRootPath, "Templates", "RabbitMqPickupTaskEvent.json"),
            Path.Combine(webHostEnvironment.WebRootPath ?? string.Empty, "Templates", "RabbitMqPickupTaskEvent.json")
        ];

        string? filePath = candidatePaths.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new FileNotFoundException("Template RabbitMqPickupTaskEvent.json not found.");
        }

        string content = File.ReadAllText(filePath);
        return FormatTemplate(content);
    }

    private static string FormatTemplate(string templateContent)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset localStartOfDay = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

        return templateContent
            .Replace("{{datetime}}", localStartOfDay.ToString("O"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{yyyyMMdd}}", localStartOfDay.ToString("yyyyMMdd"), StringComparison.OrdinalIgnoreCase);
    }

    private static void SetValue(JsonNode node, string propertyName, object value)
    {
        if (node is not JsonObject obj || !obj.ContainsKey(propertyName))
        {
            return;
        }

        obj[propertyName] = value switch
        {
            string s => JsonValue.Create(s),
            int i => JsonValue.Create(i),
            decimal d => JsonValue.Create(d),
            long l => JsonValue.Create(l),
            double dbl => JsonValue.Create(dbl),
            bool b => JsonValue.Create(b),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static void UpdatePickupTaskIdPrefix(JsonNode node, string newPrefix)
    {
        if (node is not JsonObject obj)
        {
            return;
        }

        string currentPickupTaskId = obj["pickupTaskId"]?.GetValue<string>() ?? string.Empty;
        string[] parts = currentPickupTaskId.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string datePart = parts.Length >= 2 && parts[1].Length == 8
            ? parts[1]
            : DateTimeOffset.Now.ToString("yyyyMMdd");

        string newPickupTaskId = BuildUniquePickupTaskId(newPrefix, datePart);
        obj["pickupTaskId"] = JsonValue.Create(newPickupTaskId);
    }

    private static string BuildUniquePickupTaskId(string postOfficeCode, string datePart)
    {
        for (int i = 0; i < 20; i++)
        {
            string randomSuffix = RandomNumberGenerator.GetString(RandomChars, 6);
            string candidate = $"{postOfficeCode}-{datePart}-{randomSuffix}";

            lock (PickupTaskIdLock)
            {
                if (GeneratedPickupTaskIds.Add(candidate))
                {
                    return candidate;
                }
            }
        }

        string fallbackSuffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{postOfficeCode}-{datePart}-{fallbackSuffix}";
    }
}

public sealed record PostOfficeOption(string Code, string Name) : Infrastructure.Shared.Interfaces.IDisplaySearchItem
{
    public string GetDisplayString() => $"{Code} - {Name}";

    public override string ToString() => $"{Code} - {Name}";

    public bool Like(string searchTerm)
    {
        return Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record EnumOption(string Value, string Description) : Infrastructure.Shared.Interfaces.IDisplaySearchItem
{
    public string GetDisplayString() => Description;

    public bool Like(string searchTerm)
    {
        return Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => Description;
}

public sealed record CreatePickupTaskEventInput
{
    public string? PostOfficeCode { get; set; }
    public string? PostOfficeName { get; set; }
    public string? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
    public int? DispatchType { get; set; }
    public int? DispatchMethod { get; set; }
    public DateTime? ScheduledPickupDate { get; set; }
    public List<PickupTaskEventOrderDto> SelectedOrders { get; set; } = [];
}
