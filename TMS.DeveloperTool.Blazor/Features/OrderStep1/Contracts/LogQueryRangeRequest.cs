using System.Text.Json.Serialization;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;

public sealed class LogQueryRangeRequest
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "v1";

    [JsonPropertyName("start")]
    public long Start { get; set; }

    [JsonPropertyName("end")]
    public long End { get; set; }

    [JsonPropertyName("requestType")]
    public string RequestType { get; set; } = "raw";

    [JsonPropertyName("compositeQuery")]
    public LogCompositeQuery CompositeQuery { get; set; } = new();

    [JsonPropertyName("formatOptions")]
    public LogFormatOptions FormatOptions { get; set; } = new();

    [JsonPropertyName("variables")]
    public Dictionary<string, object> Variables { get; set; } = [];
}

public sealed class LogCompositeQuery
{
    [JsonPropertyName("queries")]
    public List<LogBuilderQuery> Queries { get; set; } = [];
}

public sealed class LogBuilderQuery
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "builder_query";

    [JsonPropertyName("spec")]
    public LogQuerySpec Spec { get; set; } = new();
}

public sealed class LogQuerySpec
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "A";

    [JsonPropertyName("signal")]
    public string Signal { get; set; } = "logs";

    [JsonPropertyName("stepInterval")]
    public int StepInterval { get; set; } = 60;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("filter")]
    public LogFilter Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("order")]
    public List<LogOrder> Order { get; set; } = [];

    [JsonPropertyName("having")]
    public LogHaving Having { get; set; } = new();
}

public sealed class LogFilter
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;
}

public sealed class LogOrder
{
    [JsonPropertyName("key")]
    public LogOrderKey Key { get; set; } = new();

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "desc";
}

public sealed class LogOrderKey
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class LogHaving
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;
}

public sealed class LogFormatOptions
{
    [JsonPropertyName("formatTableResultForUI")]
    public bool FormatTableResultForUI { get; set; }

    [JsonPropertyName("fillGaps")]
    public bool FillGaps { get; set; }
}
