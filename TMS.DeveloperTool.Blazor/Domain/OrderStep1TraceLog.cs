namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class OrderStep1TraceLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }
    public string MessageDetail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
