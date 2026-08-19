using System.Text.Json;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.OrderStep1;

public class LogQueryServiceTests
{
    private const string SampleResponseJson = """
    {
      "status": "success",
      "data": {
        "type": "raw",
        "meta": { "rowsScanned": 118843, "bytesScanned": 1788160, "durationMs": 223 },
        "data": {
          "results": [
            {
              "queryName": "A",
              "nextCursor": "",
              "rows": [
                {
                  "data": {
                    "attributes_string": {
                      "AGG": "TOK2602628",
                      "OrderId": "TOK2602628",
                      "MessageDetail": "{\n  \"EventType\": \"RabbitMqOrderEvent\",\n  \"OrderId\": \"TOK2602628\"\n}"
                    },
                    "id": "0lWosVEWAs0gEneqWZIAYMkGnZf",
                    "scope_name": "TMS.OrderService.Application.Services.Step1.RabbitMqOrderMessageHandler",
                    "span_id": "d8cdc6359866b443",
                    "trace_id": "6818c64b8941e80a6be8444ba31054c7"
                  },
                  "timestamp": "2026-08-19T20:25:01.4056536+07:00"
                }
              ]
            }
          ]
        }
      }
    }
    """;

    [Fact]
    public void Deserialize_ShouldParseNestedResultsAndSnakeCaseFields()
    {
        LogQueryRangeResponse? response = JsonSerializer.Deserialize<LogQueryRangeResponse>(SampleResponseJson);

        response.Should().NotBeNull();
        List<LogRow> rows = response!.Data!.Data!.Results.Single().Rows;
        rows.Should().ContainSingle();

        LogRow row = rows[0];
        row.Data!.Id.Should().Be("0lWosVEWAs0gEneqWZIAYMkGnZf");
        row.Data.TraceId.Should().Be("6818c64b8941e80a6be8444ba31054c7");
        row.Data.SpanId.Should().Be("d8cdc6359866b443");
        row.Timestamp.Should().Be(DateTimeOffset.Parse("2026-08-19T20:25:01.4056536+07:00"));
        row.Data.AttributesString["OrderId"].Should().Be("TOK2602628");
        row.Data.AttributesString["MessageDetail"].Should().Contain("RabbitMqOrderEvent");
    }

    [Fact]
    public void ToEntry_ShouldExtractOrderIdAndMessageDetailFromAttributes()
    {
        LogQueryRangeResponse response = JsonSerializer.Deserialize<LogQueryRangeResponse>(SampleResponseJson)!;
        LogRow row = response.Data!.Data!.Results.Single().Rows.Single();

        OrderStep1TraceLogEntry? entry = LogQueryService.ToEntry(row);

        entry.Should().NotBeNull();
        entry!.LogId.Should().Be("0lWosVEWAs0gEneqWZIAYMkGnZf");
        entry.OrderId.Should().Be("TOK2602628");
        entry.TraceId.Should().Be("6818c64b8941e80a6be8444ba31054c7");
        entry.SpanId.Should().Be("d8cdc6359866b443");
        entry.MessageDetail.Should().Contain("RabbitMqOrderEvent");
    }

    [Fact]
    public void ToEntry_ShouldReturnNull_WhenRowDataMissing()
    {
        LogRow row = new() { Data = null };

        OrderStep1TraceLogEntry? entry = LogQueryService.ToEntry(row);

        entry.Should().BeNull();
    }
}
