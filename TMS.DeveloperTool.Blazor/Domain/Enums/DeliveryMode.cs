using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DeliveryMode
{
    [Description("Sổ phát toàn bộ đơn")]
    FullOrder = 1,

    [Description("Sổ phát một phần đơn")]
    PartialOrder = 2,
}
