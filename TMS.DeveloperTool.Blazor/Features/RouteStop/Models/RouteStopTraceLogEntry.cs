namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Models;

public sealed record RouteStopTraceLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string Action,
    string Driver,
    string Office,
    string EventId,
    string VehicleId,
    string AssignmentId,
    string MessageDetail);
