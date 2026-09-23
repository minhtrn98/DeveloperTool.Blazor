namespace TMS.DeveloperTool.Blazor.Domain.Enums;

/// <summary>
/// Status of an order (and its items) within a pickup task, as a smart enum: each status carries
/// its code, name and description.
/// </summary>
public sealed class PickupTaskOrderStatus : IEquatable<PickupTaskOrderStatus>
{
    // Declared before the instances so the constructor can register into it.
    private static readonly List<PickupTaskOrderStatus> Items = [];

    public static readonly PickupTaskOrderStatus New = new(0, nameof(New), "Khởi tạo");
    public static readonly PickupTaskOrderStatus Picked = new(1, nameof(Picked), "Đã lấy");
    public static readonly PickupTaskOrderStatus Splited = new(2, nameof(Splited), "Đã tách");
    public static readonly PickupTaskOrderStatus Cancelled = new(9, nameof(Cancelled), "Hủy");

    private PickupTaskOrderStatus(int value, string name, string description)
    {
        Value = value;
        Name = name;
        Description = description;
        Items.Add(this);
    }

    public int Value { get; }
    public string Name { get; }
    public string Description { get; }

    public static IReadOnlyList<PickupTaskOrderStatus> List => Items;

    public static PickupTaskOrderStatus FromValue(int value)
        => Items.FirstOrDefault(item => item.Value == value)
           ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {nameof(PickupTaskOrderStatus)} value.");

    public static PickupTaskOrderStatus FromName(string name)
        => Items.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
           ?? throw new ArgumentOutOfRangeException(nameof(name), name, $"Unknown {nameof(PickupTaskOrderStatus)} name.");

    public static PickupTaskOrderStatus FromDescription(string description)
        => Items.FirstOrDefault(item => item.Description == description)
           ?? throw new ArgumentOutOfRangeException(nameof(description), description, $"Unknown {nameof(PickupTaskOrderStatus)} description.");

    public bool Equals(PickupTaskOrderStatus? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is PickupTaskOrderStatus other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Description;

    public static bool operator ==(PickupTaskOrderStatus? left, PickupTaskOrderStatus? right) => Equals(left, right);

    public static bool operator !=(PickupTaskOrderStatus? left, PickupTaskOrderStatus? right) => !Equals(left, right);
}
