using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DispatchMethod
{
    [Description("Điều nhận")]
    Pickup = 1,

    [Description("Điều chở")]
    Delivery = 2
}
