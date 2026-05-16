using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum OrderType
{
    [Description("Đơn thường")]
    Regular = 1,

    [Description("Đơn COD")]
    Cod = 2,
}
