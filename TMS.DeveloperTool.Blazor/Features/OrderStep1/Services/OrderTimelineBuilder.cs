using System.Text.Json;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

public static class OrderTimelineBuilder
{
    private static readonly HashSet<string> ExcludedFields = new(StringComparer.Ordinal) { "Id", "EventId", "Timestamp" };
    private static readonly string[] ItemIdentityFieldCandidates = ["OrderItemId", "Id", "ItemId"];
    private static readonly string[] ItemStatusFieldCandidates = ["CurrentStatusName", "CurrentStatusId"];
    private const string OrderItemsField = "OrderItems";

    public static OrderTimeline Build(IReadOnlyList<OrderStep1TraceLog> logs)
    {
        List<DateTimeOffset> timestamps = logs.Select(log => log.LogTimestamp).ToList();
        List<Dictionary<string, string?>> snapshots = logs.Select(ParseSnapshot).ToList();
        List<OrderFieldTimelineRow> rows = BuildRows(snapshots);

        List<OrderStep1TraceLog> itemStatusLogs = GroupItemStatusLogsBySecond(logs);
        List<DateTimeOffset> itemStatusTimestamps = itemStatusLogs.Select(log => log.LogTimestamp).ToList();
        List<OrderFieldTimelineRow> itemStatusRows = BuildItemStatusRows(itemStatusLogs);

        return new OrderTimeline(timestamps, rows, itemStatusTimestamps, itemStatusRows);
    }

    // Buckets logs to 1-second resolution, then drops buckets whose item statuses repeat the previous kept bucket.
    private static List<OrderStep1TraceLog> GroupItemStatusLogsBySecond(IReadOnlyList<OrderStep1TraceLog> logs)
    {
        List<OrderStep1TraceLog> perSecond = [];
        foreach (OrderStep1TraceLog log in logs.OrderBy(l => l.LogTimestamp))
        {
            if (perSecond.Count > 0 && TruncateToSecond(perSecond[^1].LogTimestamp) == TruncateToSecond(log.LogTimestamp))
            {
                perSecond[^1] = log;
                continue;
            }

            perSecond.Add(log);
        }

        List<OrderStep1TraceLog> deduped = [];
        Dictionary<string, string?>? previousStatuses = null;
        foreach (OrderStep1TraceLog log in perSecond)
        {
            Dictionary<string, string?> statuses = GetItemStatuses(log);
            if (previousStatuses is not null && StatusesEqual(previousStatuses, statuses))
            {
                continue;
            }

            deduped.Add(log);
            previousStatuses = statuses;
        }

        return deduped;
    }

    private static Dictionary<string, string?> GetItemStatuses(OrderStep1TraceLog log)
    {
        return ParseOrderItems(log.MessageDetail)
            .ToDictionary(item => item.ItemId, item => ResolveItemStatus(item.Snapshot), StringComparer.Ordinal);
    }

    private static bool StatusesEqual(Dictionary<string, string?> left, Dictionary<string, string?> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach ((string itemId, string? status) in left)
        {
            if (!right.TryGetValue(itemId, out string? otherStatus) || status != otherStatus)
            {
                return false;
            }
        }

        return true;
    }

    private static DateTimeOffset TruncateToSecond(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);
    }

    private static List<OrderFieldTimelineRow> BuildItemStatusRows(IReadOnlyList<OrderStep1TraceLog> logs)
    {
        List<string> itemOrder = [];
        Dictionary<string, string?[]> statusByItem = new(StringComparer.Ordinal);

        for (int logIndex = 0; logIndex < logs.Count; logIndex++)
        {
            foreach ((string itemId, Dictionary<string, string?> snapshot) in ParseOrderItems(logs[logIndex].MessageDetail))
            {
                if (!statusByItem.TryGetValue(itemId, out string?[]? statuses))
                {
                    statuses = new string?[logs.Count];
                    statusByItem[itemId] = statuses;
                    itemOrder.Add(itemId);
                }

                statuses[logIndex] = ResolveItemStatus(snapshot);
            }
        }

        return itemOrder
            .Select(itemId => new OrderFieldTimelineRow(itemId, statusByItem[itemId].ToList()))
            .ToList();
    }

    private static string? ResolveItemStatus(Dictionary<string, string?> snapshot)
    {
        return ItemStatusFieldCandidates
            .Select(key => snapshot.GetValueOrDefault(key))
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));
    }

    private static List<OrderFieldTimelineRow> BuildRows(List<Dictionary<string, string?>> snapshots)
    {
        List<string> fieldOrder = [];
        HashSet<string> seenFields = new(StringComparer.Ordinal);
        foreach (Dictionary<string, string?> snapshot in snapshots)
        {
            foreach (string field in snapshot.Keys)
            {
                if (seenFields.Add(field))
                {
                    fieldOrder.Add(field);
                }
            }
        }

        bool onlyChangedFields = snapshots.Count > 1;
        List<OrderFieldTimelineRow> rows = [];
        foreach (string field in fieldOrder)
        {
            List<string?> values = snapshots.Select(snapshot => snapshot.GetValueOrDefault(field)).ToList();
            if (onlyChangedFields && !HasChange(values))
            {
                continue;
            }

            rows.Add(new OrderFieldTimelineRow(field, values));
        }

        return rows;
    }

    private static bool HasChange(List<string?> values)
    {
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] != values[i - 1])
            {
                return true;
            }
        }

        return false;
    }

    internal static Dictionary<string, string?> ParseSnapshot(OrderStep1TraceLog log)
    {
        Dictionary<string, string?> snapshot = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(log.MessageDetail))
        {
            return snapshot;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(log.MessageDetail);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (ExcludedFields.Contains(property.Name))
                {
                    continue;
                }

                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    continue;
                }

                snapshot[property.Name] = ReadScalar(property.Value);
            }
        }
        catch (JsonException)
        {
            // ignore malformed message detail
        }

        return snapshot;
    }

    internal static List<(string ItemId, Dictionary<string, string?> Snapshot)> ParseOrderItems(string messageDetail)
    {
        List<(string ItemId, Dictionary<string, string?> Snapshot)> items = [];
        if (string.IsNullOrWhiteSpace(messageDetail))
        {
            return items;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(messageDetail);
            if (!document.RootElement.TryGetProperty(OrderItemsField, out JsonElement orderItems)
                || orderItems.ValueKind != JsonValueKind.Array)
            {
                return items;
            }

            int index = 0;
            foreach (JsonElement itemElement in orderItems.EnumerateArray())
            {
                if (itemElement.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, string?> snapshot = new(StringComparer.Ordinal);
                    foreach (JsonProperty property in itemElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            continue;
                        }

                        snapshot[property.Name] = ReadScalar(property.Value);
                    }

                    string itemId = ItemIdentityFieldCandidates
                        .Select(key => snapshot.GetValueOrDefault(key))
                        .FirstOrDefault(value => !string.IsNullOrEmpty(value)) ?? $"#{index}";

                    items.Add((itemId, snapshot));
                }

                index++;
            }
        }
        catch (JsonException)
        {
            // ignore malformed message detail
        }

        return items;
    }

    private static string? ReadScalar(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => element.GetRawText()
        };
    }
}
