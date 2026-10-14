namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>"[ORDER-LOCATION] Upserted" events — vị trí giao được cấu hình của đơn, synced from SigNoz Pro (pro schema only).</summary>
public sealed class OrderLocationLog
{
    public long Id { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public DateTimeOffset LogTimestamp { get; set; }

    /// <summary>Mã đơn ({OrderId}).</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Tháng ({Month}).</summary>
    public DateOnly? Month { get; set; }

    /// <summary>Vĩ độ ({Lat}, đọc từ body để giữ đủ độ chính xác).</summary>
    public decimal? Lat { get; set; }

    /// <summary>Kinh độ ({Long}, đọc từ body để giữ đủ độ chính xác).</summary>
    public decimal? Lng { get; set; }

    /// <summary>Nguồn ({Source}), ví dụ PMS.</summary>
    public string Source { get; set; } = string.Empty;

    public string Env { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
