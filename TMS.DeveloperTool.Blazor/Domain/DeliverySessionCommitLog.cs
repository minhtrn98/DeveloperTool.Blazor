namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>
/// "Tạo sổ phát bằng quét mã bàn giao" (CommitCreateDeliverySession) events — synced from SigNoz Pro (pro schema only).
/// </summary>
public sealed class DeliverySessionCommitLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Số sổ phát được tạo ({ManifestCount}).</summary>
    public int ManifestCount { get; set; }

    /// <summary>Danh sách mã sổ phát ({Codes}).</summary>
    public string[] ManifestCodes { get; set; } = [];

    /// <summary>Số kiện ({Count}).</summary>
    public int ItemCount { get; set; }

    /// <summary>Người tạo (attributes_string.User, fallback {Actor}).</summary>
    public string Actor { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
