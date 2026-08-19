namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;

public sealed record OrderStep1TraceLogEntry(
    string LogId,
    string OrderId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string MessageDetail);
