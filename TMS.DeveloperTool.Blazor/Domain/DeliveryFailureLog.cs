namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>"Giao thất bại" (RecordDeliveryFailure) events — synced from SigNoz Pro (pro schema only).</summary>
public sealed class DeliveryFailureLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã sổ phát ({DE}).</summary>
    public string DeliveryManifestCode { get; set; } = string.Empty;

    /// <summary>Mã bản ghi giao thất bại ({RecordId}).</summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>Loại giao thất bại ({Type}).</summary>
    public string FailureType { get; set; } = string.Empty;

    /// <summary>Số task ({TaskCount}).</summary>
    public int TaskCount { get; set; }

    /// <summary>Số sổ phát ({ManifestCount}).</summary>
    public int ManifestCount { get; set; }

    /// <summary>Số kiện ({Count}).</summary>
    public int ItemCount { get; set; }

    /// <summary>Tài xế ({DriverId}).</summary>
    public string DriverId { get; set; } = string.Empty;

    /// <summary>Người ghi nhận (attributes_string.User).</summary>
    public string Actor { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
