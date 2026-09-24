namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>"Xác nhận phát hàng" (CompleteDeliveryTask) events — synced from SigNoz Pro (pro schema only).</summary>
public sealed class DeliveryTaskCompleteLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã sổ phát ({DE}).</summary>
    public string DeliveryManifestCode { get; set; } = string.Empty;

    /// <summary>Số task ({TaskCount}).</summary>
    public int TaskCount { get; set; }

    /// <summary>Số sổ phát ({ManifestCount}).</summary>
    public int ManifestCount { get; set; }

    /// <summary>Số kiện đã phát ({Delivered}).</summary>
    public int DeliveredCount { get; set; }

    /// <summary>Số tiền COD đã thu ({Cod}).</summary>
    public decimal CollectedCod { get; set; }

    /// <summary>Người xác nhận phát hàng (attributes_string.User).</summary>
    public string Actor { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
