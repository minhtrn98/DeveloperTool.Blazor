namespace TMS.DeveloperTool.Blazor.Domain.Enums;

/// <summary>
/// Order status as a smart enum: each status carries its code, name and Vietnamese description.
/// </summary>
public sealed class OrderStatus : IEquatable<OrderStatus>
{
    // Declared before the instances so the constructor can register into it.
    private static readonly List<OrderStatus> Items = [];

    public static readonly OrderStatus Initialized = new(0, nameof(Initialized), "Khởi tạo");
    public static readonly OrderStatus InternalTransferring = new(1, nameof(InternalTransferring), "Đang gởi nội bộ");
    public static readonly OrderStatus Packing = new(2, nameof(Packing), "Đóng gói");
    public static readonly OrderStatus InTransit = new(3, nameof(InTransit), "Đang vận chuyển");
    public static readonly OrderStatus Received = new(4, nameof(Received), "Đã nhận");
    public static readonly OrderStatus Delivering = new(5, nameof(Delivering), "Đang phát");
    public static readonly OrderStatus Delivered = new(6, nameof(Delivered), "Đã phát");
    public static readonly OrderStatus Returning = new(7, nameof(Returning), "Chuyển hoàn");
    public static readonly OrderStatus MissingWaybillAfterSorting = new(8, nameof(MissingWaybillAfterSorting), "Thiếu vận đơn khi hoàn thành chia thư");
    public static readonly OrderStatus Forwarded = new(9, nameof(Forwarded), "Chuyển tiếp");
    public static readonly OrderStatus InDeliveryManifest = new(10, nameof(InDeliveryManifest), "Đang trong sổ phát");
    public static readonly OrderStatus ReceivingIncomplete = new(11, nameof(ReceivingIncomplete), "Chưa nhận xong");
    public static readonly OrderStatus DeliveryIncomplete = new(12, nameof(DeliveryIncomplete), "Chưa phát xong");
    public static readonly OrderStatus DeliveryRescheduled = new(13, nameof(DeliveryRescheduled), "Hẹn phát lại");
    public static readonly OrderStatus ReceivedExcess = new(14, nameof(ReceivedExcess), "Nhận thừa");
    public static readonly OrderStatus ReceivedAtInspectionCenter = new(15, nameof(ReceivedAtInspectionCenter), "Đã nhận ở TTKT");
    public static readonly OrderStatus PendingProcessing = new(16, nameof(PendingProcessing), "Chờ xử lý");
    public static readonly OrderStatus SentToThirdParty = new(17, nameof(SentToThirdParty), "Đã gửi bên thứ 3");
    /// <summary>"Hủy = Tiêu Hủy ở PMS".</summary>
    public static readonly OrderStatus Destroyed = new(18, nameof(Destroyed), "Hủy");
    public static readonly OrderStatus Lost = new(19, nameof(Lost), "Thất lạc");
    public static readonly OrderStatus WaitingThirdPartyReturn = new(20, nameof(WaitingThirdPartyReturn), "Chờ bên thứ 3 hoàn");
    public static readonly OrderStatus Returned = new(23, nameof(Returned), "Đã hoàn");
    public static readonly OrderStatus Confiscated = new(24, nameof(Confiscated), "Tịch thu");
    public static readonly OrderStatus SystemChecking = new(25, nameof(SystemChecking), "Hệ thống kiểm tra");
    public static readonly OrderStatus TransferDeliveryManifest = new(26, nameof(TransferDeliveryManifest), "Chuyển sổ phát");
    public static readonly OrderStatus Bagging = new(40, nameof(Bagging), "Đóng tải");
    public static readonly OrderStatus MasterBagging = new(41, nameof(MasterBagging), "Đóng Master kiện");
    public static readonly OrderStatus Palletizing = new(42, nameof(Palletizing), "Đóng Pallet");
    public static readonly OrderStatus ReceivedMissing = new(44, nameof(ReceivedMissing), "Nhận thiếu");
    public static readonly OrderStatus PartiallyConfiscated = new(45, nameof(PartiallyConfiscated), "Tịch thu một phần");
    public static readonly OrderStatus PartiallyDestroyed = new(46, nameof(PartiallyDestroyed), "Tiêu hủy một phần");
    public static readonly OrderStatus Misrouted = new(47, nameof(Misrouted), "Lạc tuyến");
    public static readonly OrderStatus PartiallyMisrouted = new(48, nameof(PartiallyMisrouted), "Lạc tuyến một phần");

    private OrderStatus(int value, string name, string description)
    {
        Value = value;
        Name = name;
        Description = description;
        Items.Add(this);
    }

    public int Value { get; }
    public string Name { get; }
    public string Description { get; }

    public static IReadOnlyList<OrderStatus> List => Items;

    public static OrderStatus FromValue(int value)
        => Items.FirstOrDefault(item => item.Value == value)
           ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {nameof(OrderStatus)} value.");

    public static OrderStatus FromName(string name)
        => Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
           ?? throw new ArgumentOutOfRangeException(nameof(name), name, $"Unknown {nameof(OrderStatus)} name.");

    public static OrderStatus FromDescription(string description)
        => Items.FirstOrDefault(item => item.Description == description)
           ?? throw new ArgumentOutOfRangeException(nameof(description), description, $"Unknown {nameof(OrderStatus)} description.");

    public bool Equals(OrderStatus? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is OrderStatus other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Description;

    public static bool operator ==(OrderStatus? left, OrderStatus? right) => Equals(left, right);

    public static bool operator !=(OrderStatus? left, OrderStatus? right) => !Equals(left, right);
}
