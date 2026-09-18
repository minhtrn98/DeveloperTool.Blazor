namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class PickupTaskTraceLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }
    public string PickupTaskId { get; set; } = string.Empty;
    public string DeliveryLineId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string MessageDetail { get; set; } = string.Empty;
    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
