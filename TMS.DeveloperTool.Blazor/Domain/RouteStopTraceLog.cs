namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class RouteStopTraceLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Office { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string VehicleId { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;
    public string MessageDetail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
