using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DeliveryTaskStatus : short
{
    [Description("Đang phát")]
    Active = 1,

    [Description("Phát thất bại")]
    Failed = 2,

    [Description("Đã phát")]
    Completed = 3,

    [Description("Hủy")]
    Cancelled = 9,
}
