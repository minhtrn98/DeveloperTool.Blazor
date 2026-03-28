using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum BusinessOperation
{
    [Description("Xuất phát")]
    Departure,

    [Description("Đi nhận")]
    Receive,

    [Description("Đi phát")]
    Delivery,

    [Description("Đón hàng")]
    Pickup,

    [Description("Gửi hàng")]
    Send,

    [Description("Kết nối")]
    Connect,

    [Description("Cứu hộ")]
    Rescue,

    [Description("Đi nhận/phát")]
    ReceiveAndDelivery,

    [Description("Đón/Gửi hàng")]
    PickupAndSend,

    [Description("Bắt đầu")]
    Start,

    [Description("Kết thúc")]
    End,

    [Description("Bảo trì")]
    Maintenance,

    [Description("Đăng kiểm")]
    Inspectation,
}