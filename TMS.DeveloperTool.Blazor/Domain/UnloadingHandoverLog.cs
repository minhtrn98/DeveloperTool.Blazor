namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>"Tạo mã bàn giao xuống hàng" (CreateUnloadingHandover) events — synced from SigNoz Pro (pro schema only).</summary>
public sealed class UnloadingHandoverLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã bàn giao xuống hàng ({HO}).</summary>
    public string HandoverCode { get; set; } = string.Empty;

    /// <summary>Id bàn giao ({Id}).</summary>
    public string HandoverId { get; set; } = string.Empty;

    /// <summary>Tài xế tạo ({DriverId}).</summary>
    public string DriverId { get; set; } = string.Empty;

    /// <summary>Số kiện ({ItemCount}).</summary>
    public int ItemCount { get; set; }

    /// <summary>Người thao tác (attributes_string.User).</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>NVKT đã nhận bàn giao — set khi có log [ReceiveUnloadingHandover] cùng {HO}.</summary>
    public bool IsReceived { get; set; }

    /// <summary>Thời điểm nhận (log_timestamp của log [ReceiveUnloadingHandover]).</summary>
    public DateTimeOffset? ReceivedAt { get; set; }

    /// <summary>Trace id của log [ReceiveUnloadingHandover].</summary>
    public string? ReceivedTraceId { get; set; }

    /// <summary>NVKT đã xác nhận kèm commit kiện — set khi có log [ConfirmUnloadingHandover] cùng {HO}.</summary>
    public bool IsConfirm { get; set; }

    /// <summary>Thời điểm xác nhận (log_timestamp của log [ConfirmUnloadingHandover]).</summary>
    public DateTimeOffset? ConfirmAt { get; set; }

    /// <summary>Trace id của log [ConfirmUnloadingHandover].</summary>
    public string? ConfirmTraceId { get; set; }

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
