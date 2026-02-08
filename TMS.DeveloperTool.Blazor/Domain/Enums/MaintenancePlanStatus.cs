using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum MaintenancePlanStatus
{
    [Description("Hủy")]
    Cancel = 0,

    [Description("Chờ bảo trì")]
    Pending = 1,

    [Description("Đang đi bảo trì")]
    InTransit = 2,

    [Description("Đang bảo trì")]
    InProgress = 3,

    [Description("Bảo trì xong")]
    Completed = 4,

    [Description("Hoàn thành")]
    Finished = 5,

    [Description("Thất bại")]
    Failed = 6
}
