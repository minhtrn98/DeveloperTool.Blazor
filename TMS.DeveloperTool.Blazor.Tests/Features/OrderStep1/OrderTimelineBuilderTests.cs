using TMS.DeveloperTool.Blazor.Domain;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.OrderStep1;

public class OrderTimelineBuilderTests
{
    private static OrderStep1TraceLog CreateLog(DateTimeOffset timestamp, string statusId, string statusName)
    {
        string messageDetail = $$"""
        {
          "EventType": "RabbitMqOrderEvent",
          "Id": "{{Guid.NewGuid()}}",
          "Timestamp": "{{timestamp:O}}",
          "EventId": "{{Guid.NewGuid()}}",
          "OrderId": "TOK2602628",
          "CurrentStatusId": "{{statusId}}",
          "CurrentStatusName": "{{statusName}}",
          "CurrentPostOfficeId": "TOK",
          "Amount": 83214.0000,
          "OrderItems": [],
          "SenderInfo": { "Address": "some address" }
        }
        """;

        return new OrderStep1TraceLog
        {
            LogId = Guid.NewGuid().ToString(),
            OrderId = "TOK2602628",
            TraceId = Guid.NewGuid().ToString(),
            SpanId = Guid.NewGuid().ToString(),
            LogTimestamp = timestamp,
            MessageDetail = messageDetail
        };
    }

    [Fact]
    public void Build_ShouldOnlyKeepFieldsThatChanged_WhenMultipleLogsExist()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLog(t1, "4", "Đã nhận"),
            CreateLog(t2, "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Timestamps.Should().Equal(t1, t2);
        timeline.Rows.Should().Contain(r => r.FieldName == "CurrentStatusId");
        timeline.Rows.Should().Contain(r => r.FieldName == "CurrentStatusName");

        OrderFieldTimelineRow statusRow = timeline.Rows.Single(r => r.FieldName == "CurrentStatusId");
        statusRow.Values.Should().Equal("4", "5");
    }

    [Fact]
    public void Build_ShouldExcludeFieldsThatNeverChange_WhenMultipleLogsExist()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLog(t1, "4", "Đã nhận"),
            CreateLog(t2, "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Rows.Should().NotContain(r => r.FieldName == "CurrentPostOfficeId");
        timeline.Rows.Should().NotContain(r => r.FieldName == "OrderId");
    }

    [Fact]
    public void Build_ShouldExcludeAdministrativeFields_EvenThoughTheyAlwaysDiffer()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLog(t1, "4", "Đã nhận"),
            CreateLog(t2, "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Rows.Should().NotContain(r => r.FieldName == "Id" || r.FieldName == "EventId" || r.FieldName == "Timestamp");
    }

    [Fact]
    public void Build_ShouldSkipArrayAndObjectFields()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs = [CreateLog(t1, "4", "Đã nhận")];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Rows.Should().NotContain(r => r.FieldName == "OrderItems" || r.FieldName == "SenderInfo");
    }

    [Fact]
    public void Build_ShouldShowAllFields_WhenOnlyOneLogExists()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs = [CreateLog(t1, "4", "Đã nhận")];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Timestamps.Should().Equal(t1);
        timeline.Rows.Should().Contain(r => r.FieldName == "CurrentStatusId");
        timeline.Rows.Should().Contain(r => r.FieldName == "CurrentPostOfficeId");
        timeline.Rows.Should().Contain(r => r.FieldName == "OrderId");
    }

    [Fact]
    public void Build_ShouldReturnEmptyRows_WhenNoLogsExist()
    {
        OrderTimeline timeline = OrderTimelineBuilder.Build([]);

        timeline.Timestamps.Should().BeEmpty();
        timeline.Rows.Should().BeEmpty();
        timeline.ItemStatusRows.Should().BeEmpty();
    }

    private static OrderStep1TraceLog CreateLogWithItem(DateTimeOffset timestamp, string orderItemId, string statusId, string statusName)
    {
        string messageDetail = $$"""
        {
          "EventType": "RabbitMqOrderEvent",
          "Id": "{{Guid.NewGuid()}}",
          "Timestamp": "{{timestamp:O}}",
          "EventId": "{{Guid.NewGuid()}}",
          "OrderId": "TOK2602628",
          "CurrentStatusId": "0",
          "CurrentStatusName": "Khởi tạo",
          "OrderItems": [
            {
              "OrderItemId": "{{orderItemId}}",
              "Weight": 1.5,
              "CurrentStatusId": "{{statusId}}",
              "CurrentStatusName": "{{statusName}}"
            }
          ]
        }
        """;

        return new OrderStep1TraceLog
        {
            LogId = Guid.NewGuid().ToString(),
            OrderId = "TOK2602628",
            TraceId = Guid.NewGuid().ToString(),
            SpanId = Guid.NewGuid().ToString(),
            LogTimestamp = timestamp,
            MessageDetail = messageDetail
        };
    }

    [Fact]
    public void Build_ShouldProduceOneRowPerItem_WithStatusAlignedToOrderTimestamps()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(t1, "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(t2, "OI-001", "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Timestamps.Should().Equal(t1, t2);
        timeline.ItemStatusRows.Should().ContainSingle();

        OrderFieldTimelineRow itemRow = timeline.ItemStatusRows[0];
        itemRow.FieldName.Should().Be("OI-001");
        itemRow.Values.Should().Equal("Đã nhận", "Đang phát");
    }

    [Fact]
    public void Build_ShouldKeepMultipleItemsAsSeparateRows_WhenDifferentOrderItemIdsAppear()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 10, 0, 1, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(t1, "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(t2, "OI-002", "4", "Đã nhận")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.ItemStatusRows.Select(r => r.FieldName).Should().Equal("OI-001", "OI-002");
    }

    [Fact]
    public void Build_ShouldFillMissingCellWithNull_WhenItemMissingFromALaterLog()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(t1, "OI-001", "4", "Đã nhận"),
            CreateLog(t2, "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        OrderFieldTimelineRow itemRow = timeline.ItemStatusRows.Single(r => r.FieldName == "OI-001");
        itemRow.Values.Should().Equal("Đã nhận", null);
    }

    [Fact]
    public void Build_ShouldKeepItemRow_EvenWhenItsOnlyColumnIsCollapsedByDedup()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(t1, "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(t2, "OI-001", "4", "Đã nhận")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        OrderFieldTimelineRow itemRow = timeline.ItemStatusRows.Single(r => r.FieldName == "OI-001");
        itemRow.Values.Should().Equal("Đã nhận");
    }

    [Fact]
    public void Build_ShouldReturnNoItemStatusRows_WhenOrderItemsIsEmpty()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs = [CreateLog(t1, "4", "Đã nhận")];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.ItemStatusRows.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldGroupLogsWithinTheSameSecond_KeepingOnlyTheLatestStatus()
    {
        DateTimeOffset baseTime = new(2026, 8, 19, 16, 21, 11, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(baseTime.AddMilliseconds(100), "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(baseTime.AddMilliseconds(400), "OI-001", "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.ItemStatusTimestamps.Should().ContainSingle();
        OrderFieldTimelineRow itemRow = timeline.ItemStatusRows.Single(r => r.FieldName == "OI-001");
        itemRow.Values.Should().Equal("Đang phát");
    }

    [Fact]
    public void Build_ShouldCollapseConsecutiveColumnsWithSameItemStatus_KeepingOnlyTransitionPoints()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 10, 0, 1, TimeSpan.Zero);
        DateTimeOffset t3 = new(2026, 8, 19, 10, 0, 2, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(t1, "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(t2, "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(t3, "OI-001", "5", "Đang phát")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.ItemStatusTimestamps.Should().Equal(t1, t3);
        OrderFieldTimelineRow itemRow = timeline.ItemStatusRows.Single(r => r.FieldName == "OI-001");
        itemRow.Values.Should().Equal("Đã nhận", "Đang phát");
    }

    [Fact]
    public void Build_ShouldKeepOrderLevelTimestampsAndRows_UnaffectedByItemStatusGrouping()
    {
        DateTimeOffset t1 = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2026, 8, 19, 10, 0, 1, TimeSpan.Zero);
        List<OrderStep1TraceLog> logs =
        [
            CreateLogWithItem(t1, "OI-001", "4", "Đã nhận"),
            CreateLogWithItem(t2, "OI-001", "4", "Đã nhận")
        ];

        OrderTimeline timeline = OrderTimelineBuilder.Build(logs);

        timeline.Timestamps.Should().Equal(t1, t2);
        timeline.ItemStatusTimestamps.Should().Equal(t1);
    }
}
