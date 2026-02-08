using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum InspectionPlanStatus
{
    [Description("")]
    None = -1,

    [Description("Đã hủy")]
    Cancel = 0,

    [Description("Chờ duyệt")]
    WaitingApproval = 1,

    [Description("Chờ đăng kiểm")]
    WaitingForInspection = 2,

    [Description("Đang đăng kiểm")]
    InProgress = 3,

    [Description("Đăng kiểm xong")]
    InspectionCompleted = 4,

    [Description("Hoàn tất")]
    Finished = 5,

    [Description("Thất bại")]
    Fail = 6
}
