using System.ComponentModel;
using System.Runtime.Serialization;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum PickupTaskStatusId
{
    [EnumMember(Value = "waiting_for_assignment")]
    [Description("Chờ phân công")]
    WaitingForAssignment = 1,

    [EnumMember(Value = "waiting_for_acceptance")]
    [Description("Chờ tiếp nhận")]
    WaitingForAcceptance = 2,

    [EnumMember(Value = "in_transit")]
    [Description("Đang di chuyển")]
    InTransit = 3,

    [EnumMember(Value = "arrived")]
    [Description("Đã đến")]
    Arrived = 4,

    [EnumMember(Value = "picking_up")]
    [Description("Đang lấy hàng")]
    PickingUp = 5,

    [EnumMember(Value = "creating_order")]
    [Description("Đang tạo đơn")]
    CreatingOrder = 6,

    [EnumMember(Value = "completed")]
    [Description("Lấy thành công")]
    Completed = 7,

    [EnumMember(Value = "pickup_failed")]
    [Description("Chưa lấy được")]
    PickupFailed = 8,

    [EnumMember(Value = "cancelled")]
    [Description("Hủy lấy đơn")]
    Cancelled = 9,

    [EnumMember(Value = "transferred_to_post_office")]
    [Description("Chuyển bưu cục")]
    TransferredToPostOffice = 10,

    [EnumMember(Value = "waiting_for_vehicle_rental")]
    [Description("Chờ thuê xe")]
    WaitingForVehicleRental = 11,

    [EnumMember(Value = "vehicle_rental_in_progress")]
    [Description("Đang thuê xe")]
    VehicleRentalInProgress = 12
}