namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>Who committed (created) a delivery manifest — synced from SigNoz Pro (pro schema only).</summary>
public sealed class DeliveryManifestCommitLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã sổ phát chuyển phát nhanh ({DE}).</summary>
    public string DeliveryManifestCode { get; set; } = string.Empty;

    /// <summary>Mã sổ phát COD ({CodCode}).</summary>
    public string CodManifestCode { get; set; } = string.Empty;

    /// <summary>Người tạo sổ phát ({Actor}).</summary>
    public string Actor { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
