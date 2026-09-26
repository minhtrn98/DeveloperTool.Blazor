namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

/// <summary>The synced SigNoz Pro log tables the dashboard aggregates, one value per table.</summary>
public static class DashboardEventType
{
    public const string ManifestCommit = "ManifestCommit";
    public const string SessionCommit = "SessionCommit";
    public const string Arrival = "Arrival";
    public const string TaskComplete = "TaskComplete";
    public const string Failure = "Failure";
    public const string Transfer = "Transfer";
    public const string UnloadingHandover = "UnloadingHandover";

    public static readonly IReadOnlyList<EnumOption> Options =
    [
        new(ManifestCommit, "Tạo sổ phát (commit manifest)"),
        new(SessionCommit, "Tạo sổ phát bằng quét mã bàn giao"),
        new(Arrival, "Check-in điểm giao (arrival)"),
        new(TaskComplete, "Xác nhận phát hàng (task complete)"),
        new(Failure, "Giao thất bại"),
        new(Transfer, "Chuyển đơn"),
        new(UnloadingHandover, "Tạo mã bàn giao xuống hàng")
    ];

    /// <summary>Meaning of <see cref="DashboardEvent.Quantity"/> per type; null when the type has no quantity.</summary>
    public static string? QuantityLabel(string eventType) => eventType switch
    {
        SessionCommit => "Số kiện",
        TaskComplete => "Kiện đã phát",
        Failure => "Kiện thất bại",
        Transfer => "Số đơn",
        UnloadingHandover => "Số kiện",
        _ => null
    };

    public static string Describe(string eventType)
        => Options.FirstOrDefault(x => x.Value == eventType)?.Description ?? eventType;
}
