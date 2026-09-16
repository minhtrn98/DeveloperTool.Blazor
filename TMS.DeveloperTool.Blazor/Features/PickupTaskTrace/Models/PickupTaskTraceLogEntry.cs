namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;

public sealed record PickupTaskTraceLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string PickupTaskId,
    string DeliveryLineId,
    string EventId,
    string MessageDetail);
