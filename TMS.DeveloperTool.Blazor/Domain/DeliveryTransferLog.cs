namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>
/// "Chuyển đơn" events — driver → driver (CreateDeliveryTransfer) or post-office employee → driver
/// (ExternalCreateDeliveryTransfer), told apart by <see cref="SourceType"/>. Synced from SigNoz Pro (pro schema only).
/// </summary>
public sealed class DeliveryTransferLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã phiếu chuyển ({Code}).</summary>
    public string TransferCode { get; set; } = string.Empty;

    /// <summary>"Driver" (tài xế → tài xế) hoặc "Employee" (nhân viên bưu cục → tài xế).</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>Mã tài xế chuyển ({SourceDriverCode}) hoặc mã nhân viên ({EmployeeCode}).</summary>
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>Mã tài xế nhận ({TargetDriverCode}).</summary>
    public string TargetDriverCode { get; set; } = string.Empty;

    /// <summary>Danh sách đơn được chuyển ({OrderIds}).</summary>
    public string[] OrderIds { get; set; } = [];

    public int OrderCount { get; set; }

    /// <summary>Thời điểm tạo phiếu chuyển ({CreatedAt:o}).</summary>
    public DateTimeOffset? TransferredAt { get; set; }

    /// <summary>Người thao tác (attributes_string.User).</summary>
    public string Actor { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
