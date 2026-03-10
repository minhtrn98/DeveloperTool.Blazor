using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum ActionType
{
    [Description("Ghép xe")]
    Pairing = 1,

    [Description("Hủy ghép xe")]
    Unpairing = 2,

    [Description("Đổi tài lái")]
    SwapMainDriver = 3,

    [Description("Đổi tài chờ")]
    SwapSubDriver = 4,

    [Description("Ghép xe đột xuất")]
    UnexpectedPairing = 5
}
