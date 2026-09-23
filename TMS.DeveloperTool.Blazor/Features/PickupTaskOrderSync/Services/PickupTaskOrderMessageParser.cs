using System.Text.Json;
using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskOrderSync.Services;

public sealed record PickupTaskOrderParseResult(
    IReadOnlyList<PickupTaskOrder> Orders,
    IReadOnlyList<PickupTaskOrderItem> Items)
{
    public static readonly PickupTaskOrderParseResult Empty = new([], []);
}

/// <summary>
/// Turns the <c>Orders</c> array of a pickup task trace log's message detail into
/// <see cref="PickupTaskOrder"/> / <see cref="PickupTaskOrderItem"/> rows. Items have no status
/// or process-completed flag of their own in the message, so each item takes its order's.
/// </summary>
/// <remarks>
/// Messages without an <c>Orders</c> array (other pickup task events) yield no rows.
/// Malformed JSON throws <see cref="JsonException"/> and an unknown status code throws
/// <see cref="ArgumentOutOfRangeException"/>.
/// </remarks>
public static class PickupTaskOrderMessageParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static PickupTaskOrderParseResult Parse(PickupTaskTraceLog log)
    {
        if (string.IsNullOrWhiteSpace(log.MessageDetail))
        {
            return PickupTaskOrderParseResult.Empty;
        }

        PickupTaskMessage? message = JsonSerializer.Deserialize<PickupTaskMessage>(log.MessageDetail, SerializerOptions);
        if (message?.Orders is not { Count: > 0 })
        {
            return PickupTaskOrderParseResult.Empty;
        }

        List<PickupTaskOrder> orders = [];
        List<PickupTaskOrderItem> items = [];
        foreach (OrderMessage order in message.Orders)
        {
            OrderStatus status = OrderStatus.FromValue(order.Status ?? OrderStatus.Initialized.Value);
            bool isProcessCompleted = order.IsProcessCompleted ?? false;

            orders.Add(new PickupTaskOrder
            {
                TraceId = log.TraceId,
                PickupTaskId = log.PickupTaskId,
                OrderId = order.OrderId ?? string.Empty,
                ExtraServices = order.ExtraService ?? string.Empty,
                Weight = order.Weight ?? 0,
                RealWeight = order.RealWeight ?? 0,
                Status = status,
                IsProcessCompleted = isProcessCompleted,
                CreatedAt = log.LogTimestamp
            });

            foreach (OrderItemMessage item in order.Items ?? [])
            {
                items.Add(new PickupTaskOrderItem
                {
                    TraceId = log.TraceId,
                    PickupTaskId = log.PickupTaskId,
                    OrderId = item.OrderId ?? order.OrderId ?? string.Empty,
                    OrderItemId = item.OrderItemId ?? string.Empty,
                    Weight = item.Weight ?? 0,
                    RealWeight = item.RealWeight ?? 0,
                    Status = status,
                    IsProcessCompleted = isProcessCompleted,
                    CreatedAt = log.LogTimestamp
                });
            }
        }

        return new PickupTaskOrderParseResult(orders, items);
    }

    private sealed record PickupTaskMessage(List<OrderMessage>? Orders);

    private sealed record OrderMessage(
        string? OrderId,
        string? ExtraService,
        decimal? Weight,
        decimal? RealWeight,
        int? Status,
        bool? IsProcessCompleted,
        List<OrderItemMessage>? Items);

    private sealed record OrderItemMessage(
        string? OrderId,
        string? OrderItemId,
        decimal? Weight,
        decimal? RealWeight);
}
