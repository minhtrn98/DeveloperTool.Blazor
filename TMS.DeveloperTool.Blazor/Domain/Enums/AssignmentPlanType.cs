using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum AssignmentPlanType
{
    [Description("Bảo trì/Bảo dưỡng")]
    Maintenance = 1,

    [Description("Đăng kiểm")]
    Inspection = 2,

    [Description("Kế hoạch vận chuyển")]
    DailyPlan = 3,

    [Description("Bảo hiểm")]
    Insurance = 4
}