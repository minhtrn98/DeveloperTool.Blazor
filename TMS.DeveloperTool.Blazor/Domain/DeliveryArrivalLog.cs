namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>"Check-in điểm giao" (RecordDeliveryArrival) events — synced from SigNoz Pro (pro schema only).</summary>
public sealed class DeliveryArrivalLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã sổ phát ({DE}).</summary>
    public string DeliveryManifestCode { get; set; } = string.Empty;

    /// <summary>Mã check-in ({ArrivalId}).</summary>
    public string ArrivalId { get; set; } = string.Empty;

    /// <summary>Mã task ({TaskId}).</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>Mã đơn ({OrderId}).</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Mã xe ({VehicleId}).</summary>
    public string VehicleId { get; set; } = string.Empty;

    /// <summary>Khoảng cách tới địa chỉ giao, mét ({Distance}); null khi log không có.</summary>
    public decimal? DistanceMeters { get; set; }

    /// <summary>GPS hợp lệ ({IsGpsValid}); null khi log không có.</summary>
    public bool? IsGpsValid { get; set; }

    /// <summary>Check-in lại ({Superseded}); bool lưu 0/1.</summary>
    public int Superseded { get; set; }

    /// <summary>Vĩ độ thiết bị (điện thoại tài xế) lúc check-in (attributes Lat, 3 chữ số thập phân).</summary>
    public decimal? DeviceLat { get; set; }

    /// <summary>Kinh độ thiết bị (điện thoại tài xế) lúc check-in (attributes Lng, 3 chữ số thập phân).</summary>
    public decimal? DeviceLng { get; set; }

    /// <summary>Vĩ độ xe lúc check-in (Latitude trong message detail của route stop DeliveryArrival cùng trace id); null khi chưa có.</summary>
    public decimal? VehicleLat { get; set; }

    /// <summary>Kinh độ xe lúc check-in (Longitude trong message detail của route stop DeliveryArrival cùng trace id); null khi chưa có.</summary>
    public decimal? VehicleLng { get; set; }

    /// <summary>Vĩ độ cấu hình của đơn (ORDER-LOCATION {Lat}); null khi chưa có.</summary>
    public decimal? OrderLat { get; set; }

    /// <summary>Kinh độ cấu hình của đơn (ORDER-LOCATION {Long}); null khi chưa có.</summary>
    public decimal? OrderLng { get; set; }

    /// <summary>Thời điểm log ORDER-LOCATION được dùng cho <see cref="OrderLat"/>/<see cref="OrderLng"/>.</summary>
    public DateTimeOffset? OrderLocationAt { get; set; }

    /// <summary>Người thao tác (attributes_string.User).</summary>
    public string Actor { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
