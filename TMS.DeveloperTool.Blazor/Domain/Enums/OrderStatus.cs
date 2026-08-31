using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum OrderStatus
{
    [Description("Khởi tạo")]
    Initialized = 0,

    [Description("Đang gởi nội bộ")]
    InternalTransferring = 1,

    [Description("Đóng gói")]
    Packing = 2,

    [Description("Đang vận chuyển")]
    InTransit = 3,

    [Description("Đã nhận")]
    Received = 4,

    [Description("Đang phát")]
    Delivering = 5,

    [Description("Đã phát")]
    Delivered = 6,

    [Description("Chuyển hoàn")]
    Returning = 7,

    [Description("Thiếu vận đơn khi hoàn thành chia thư")]
    MissingWaybillAfterSorting = 8,

    [Description("Chuyển tiếp")]
    Forwarded = 9,

    [Description("Đang trong sổ phát")]
    InDeliveryManifest = 10,

    [Description("Chưa nhận xong")]
    ReceivingIncomplete = 11,

    [Description("Chưa phát xong")]
    DeliveryIncomplete = 12,

    [Description("Hẹn phát lại")]
    DeliveryRescheduled = 13,

    [Description("Nhận thừa")]
    ReceivedExcess = 14,

    [Description("Đã nhận ở TTKT")]
    ReceivedAtInspectionCenter = 15,

    [Description("Chờ xử lý")]
    PendingProcessing = 16,

    [Description("Đã gửi bên thứ 3")]
    SentToThirdParty = 17,

    [Description("Hủy")]
    Cancelled = 18,

    [Description("Thất lạc")]
    Lost = 19,

    [Description("Chờ bên thứ 3 hoàn")]
    WaitingThirdPartyReturn = 20,

    [Description("Đã hoàn")]
    Returned = 23,

    [Description("Tịch thu")]
    Confiscated = 24,

    [Description("Hệ thống kiểm tra")]
    SystemChecking = 25,

    [Description("Chuyển sổ phát")]
    TransferDeliveryManifest = 26,

    [Description("Đóng tải")]
    Bagging = 40,

    [Description("Đóng Master kiện")]
    MasterBagging = 41,

    [Description("Đóng Pallet")]
    Palletizing = 42,

    [Description("Nhận thiếu")]
    ReceivedMissing = 44,

    [Description("Tịch thu một phần")]
    PartiallyConfiscated = 45,

    [Description("Tiêu hủy một phần")]
    PartiallyDestroyed = 46,

    [Description("Lạc tuyến")]
    Misrouted = 47,

    [Description("Lạc tuyến một phần")]
    PartiallyMisrouted = 48,

    [Description("Chờ lên hàng")]
    Planned = 100,

    [Description("Đang trên xe")]
    Loaded = 101,
}
