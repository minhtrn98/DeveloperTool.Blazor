namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;

public sealed record OrderFieldTimelineRow(string FieldName, List<string?> Values);

public sealed record OrderTimeline(
    List<DateTimeOffset> Timestamps,
    List<OrderFieldTimelineRow> Rows,
    List<DateTimeOffset> ItemStatusTimestamps,
    List<OrderFieldTimelineRow> ItemStatusRows);
