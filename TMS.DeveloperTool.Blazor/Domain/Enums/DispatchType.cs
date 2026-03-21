using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DispatchType
{
    [Description("Đi nhận - KH bưu cục")]
    PostOfficeCustomer = 1,

    [Description("Đi nhận - KH hệ thống")]
    SystemCustomer = 2,

    [Description("Đi nhận - nhận hộ")]
    ProxyPickup = 3,

    [Description("Đi nhận - khách lẻ")]
    RetailCustomer = 4,

    [Description("Đi nhận - KH Web/API")]
    WebApiCustomer = 5,

    [Description("Gửi hàng")]
    Send = 6,

    [Description("Đón hàng")]
    Pickup = 7,

    [Description("Cứu hộ hàng")]
    Rescue = 8,

    [Description("Kết nối")]
    Connect = 9,

    [Description("Nối chuyến")]
    Transshipment = 10
}
