using MudBlazor;

namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Components;

/// <summary>Shared axis options for dashboard charts.</summary>
public static class DashboardChartOptions
{
    /// <summary>
    /// MudBlazor draws axis charts in a fixed 650×350 viewBox by default, so with a fixed Height the
    /// chart scales to ~650px wide and sits centered in a wider container.
    /// <see cref="AxisChartOptions.MatchBoundsToSize"/> makes it fill the container width instead.
    /// Returns a new instance per chart.
    /// </summary>
    public static AxisChartOptions Axis(bool rotateLabels = false) => new()
    {
        MatchBoundsToSize = true,
        XAxisLabelRotation = rotateLabels ? 45 : 0
    };
}
