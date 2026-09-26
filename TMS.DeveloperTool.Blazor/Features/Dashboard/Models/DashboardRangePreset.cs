namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

/// <summary>Quick date ranges for the dashboard; <see cref="Custom"/> leaves the picked range untouched.</summary>
public static class DashboardRangePreset
{
    public const string Today = "Today";
    public const string Last7Days = "Last7Days";
    public const string Last30Days = "Last30Days";
    public const string ThisMonth = "ThisMonth";
    public const string LastMonth = "LastMonth";
    public const string Last90Days = "Last90Days";
    public const string Custom = "Custom";

    public static readonly IReadOnlyList<EnumOption> Options =
    [
        new(Today, "Hôm nay"),
        new(Last7Days, "7 ngày gần nhất"),
        new(Last30Days, "30 ngày gần nhất"),
        new(ThisMonth, "Tháng này"),
        new(LastMonth, "Tháng trước"),
        new(Last90Days, "90 ngày gần nhất"),
        new(Custom, "Tùy chọn khoảng")
    ];

    /// <summary>Inclusive start/end dates (Vietnam calendar) for a preset, or null for <see cref="Custom"/>.</summary>
    public static (DateOnly Start, DateOnly End)? Resolve(string preset, DateOnly today)
    {
        DateOnly firstOfMonth = new(today.Year, today.Month, 1);

        return preset switch
        {
            Today => (today, today),
            Last7Days => (today.AddDays(-6), today),
            Last30Days => (today.AddDays(-29), today),
            ThisMonth => (firstOfMonth, today),
            LastMonth => (firstOfMonth.AddMonths(-1), firstOfMonth.AddDays(-1)),
            Last90Days => (today.AddDays(-89), today),
            _ => null
        };
    }
}
