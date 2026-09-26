namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

/// <summary>Bucket size for the trend charts. Weeks start on Monday; all buckets use Vietnam time.</summary>
public static class DashboardGranularity
{
    public const string Day = "Day";
    public const string Week = "Week";
    public const string Month = "Month";

    public static readonly IReadOnlyList<EnumOption> Options =
    [
        new(Day, "Theo ngày"),
        new(Week, "Theo tuần"),
        new(Month, "Theo tháng")
    ];
}
