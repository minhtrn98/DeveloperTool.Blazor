using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DeliveryItemStatus : short
{
    [Description("Chờ lên hàng")]
    Planned = 0,

    [Description("Đang trên xe")]
    Loaded = 1,

    [Description("Hẹn phát lại")]
    DeliveryRescheduled = 2,

    [Description("Chờ xử lý")]
    PendingProcessing = 3,

    [Description("Đã phát")]
    Delivered = 4,

    [Description("Chuyển hoàn")]
    Returning = 5,

    [Description("Chuyển tiếp")]
    Forwarded = 6,

    [Description("Thất lạc")]
    Lost = 7,

    [Description("Tịch thu")]
    Confiscated = 8,

    [Description("Tiêu hủy")]
    Destroyed = 9,

    [Description("Đã gỡ")]
    Removed = 11,
}
