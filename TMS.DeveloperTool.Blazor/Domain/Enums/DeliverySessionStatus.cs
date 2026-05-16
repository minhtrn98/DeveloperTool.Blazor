using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DeliverySessionStatus : short
{
    [Description("Đang phát")]
    Active = 1,

    [Description("Chưa phát xong")]
    PartiallyCompleted = 2,

    [Description("Đã phát")]
    Completed = 3,

    [Description("Hủy")]
    Cancelled = 9,
}
