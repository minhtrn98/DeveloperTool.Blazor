using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DeliveryManifestStatus : short
{
    [Description("Khởi tạo")]
    Initialized = 0,

    [Description("Đang phát")]
    Active = 1,

    [Description("Chưa phát xong")]
    PartiallyDelivered = 2,

    [Description("Đã phát")]
    Completed = 3,

    [Description("Hủy")]
    Cancelled = 9,
}
