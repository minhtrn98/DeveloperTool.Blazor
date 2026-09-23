using System.Text.Json;
using TMS.DeveloperTool.Blazor.Domain;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Features.PickupTaskOrderSync.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.PickupTaskOrderSync;

public class PickupTaskOrderMessageParserTests
{
    private static readonly DateTimeOffset LogTimestamp = new(2026, 9, 21, 8, 6, 17, TimeSpan.Zero);

    private const string SampleMessage = """
        {
          "IsRescheduledAll": false,
          "IsRedispatch": false,
          "Orders": [
            {
              "OrderId": "SOCAC1497198",
              "CreatedAt": "2026-09-21T08:06:16.919Z",
              "ServiceTypeId": "TF",
              "ExtraService": "TKKH,PHST",
              "Weight": 16.0,
              "RealWeight": 15.5,
              "DeclaredValue": 5000000.000,
              "Status": 6,
              "Items": [
                { "OrderId": "SOCAC1497198", "OrderItemId": "SOCAC1497198-1", "Weight": 4.0, "RealWeight": 3.5, "Note": null },
                { "OrderId": "SOCAC1497198", "OrderItemId": "SOCAC1497198-2", "Weight": 12.0, "RealWeight": 12.0, "Note": null }
              ],
              "IsProcessCompleted": true
            }
          ],
          "ActualPickedItems": null,
          "TraceId": "b8816ab3-a17a-4625-9e93-ef1fd42c4549"
        }
        """;

    private static PickupTaskTraceLog CreateLog(string messageDetail) => new()
    {
        TraceId = "trace-1",
        PickupTaskId = "PT001",
        LogTimestamp = LogTimestamp,
        MessageDetail = messageDetail
    };

    [Fact]
    public void Parse_ShouldMapOrderFromMessageAndLog()
    {
        PickupTaskOrderParseResult result = PickupTaskOrderMessageParser.Parse(CreateLog(SampleMessage));

        PickupTaskOrder order = result.Orders.Should().ContainSingle().Subject;
        order.TraceId.Should().Be("trace-1");
        order.PickupTaskId.Should().Be("PT001");
        order.OrderId.Should().Be("SOCAC1497198");
        order.ExtraServices.Should().Be("TKKH,PHST");
        order.Weight.Should().Be(16.0m);
        order.RealWeight.Should().Be(15.5m);
        order.Status.Should().Be(OrderStatus.Delivered);
        order.IsProcessCompleted.Should().BeTrue();
        order.CreatedAt.Should().Be(LogTimestamp);
    }

    [Fact]
    public void Parse_ShouldMapItemsWithTheirOrderStatus()
    {
        PickupTaskOrderParseResult result = PickupTaskOrderMessageParser.Parse(CreateLog(SampleMessage));

        result.Items.Should().HaveCount(2);
        result.Items.Select(item => item.OrderItemId).Should().Equal("SOCAC1497198-1", "SOCAC1497198-2");
        result.Items[0].TraceId.Should().Be("trace-1");
        result.Items[0].PickupTaskId.Should().Be("PT001");
        result.Items[0].OrderId.Should().Be("SOCAC1497198");
        result.Items[0].Weight.Should().Be(4.0m);
        result.Items[0].RealWeight.Should().Be(3.5m);
        result.Items.Should().OnlyContain(item => item.Status == OrderStatus.Delivered && item.IsProcessCompleted && item.CreatedAt == LogTimestamp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("""{ "PmsStatusID": 1, "DeliveryLineID": "DL1" }""")]
    [InlineData("""{ "Orders": null }""")]
    [InlineData("""{ "Orders": [] }""")]
    public void Parse_ShouldReturnNoRows_WhenMessageHasNoOrders(string messageDetail)
    {
        PickupTaskOrderParseResult result = PickupTaskOrderMessageParser.Parse(CreateLog(messageDetail));

        result.Orders.Should().BeEmpty();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldThrowJsonException_WhenMessageIsNotJson()
    {
        Action act = () => PickupTaskOrderMessageParser.Parse(CreateLog("not json"));

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Parse_ShouldThrow_WhenStatusIsUnknown()
    {
        Action act = () => PickupTaskOrderMessageParser.Parse(CreateLog("""{ "Orders": [ { "OrderId": "O1", "Status": 999 } ] }"""));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
