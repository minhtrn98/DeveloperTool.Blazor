using System.Text.Json.Serialization;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;

public sealed class LogQueryRangeResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public LogQueryRangePayload? Data { get; set; }
}

public sealed class LogQueryRangePayload
{
    [JsonPropertyName("data")]
    public LogQueryRangeResultSet? Data { get; set; }
}

public sealed class LogQueryRangeResultSet
{
    [JsonPropertyName("results")]
    public List<LogQueryResult> Results { get; set; } = [];
}

public sealed class LogQueryResult
{
    [JsonPropertyName("rows")]
    public List<LogRow> Rows { get; set; } = [];
}

public sealed class LogRow
{
    [JsonPropertyName("data")]
    public LogRowData? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class LogRowData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("trace_id")]
    public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("span_id")]
    public string SpanId { get; set; } = string.Empty;

    [JsonPropertyName("attributes_string")]
    public Dictionary<string, string> AttributesString { get; set; } = [];
}
