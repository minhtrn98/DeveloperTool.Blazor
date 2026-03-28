namespace TMS.DeveloperTool.Blazor.Domain.Enums;

/// <summary>
/// Pickup confirmation modes.
/// </summary>
public enum PickupMode
{
    /// <summary>Lấy tổng — pick up everything in the task.</summary>
    Full = 1,

    /// <summary>Lấy theo từng đơn/ kiện — pick up selected orders/orderItems</summary>
    ByOrder = 2,
}
