using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class DeliveryArrivalLocationFillOptions
{
    public const string SectionName = "DeliveryArrivalLocationFill";

    /// <summary>Minutes between fill runs.</summary>
    [Range(1, 1440)]
    public int IntervalMinutes { get; init; } = 5;

    /// <summary>
    /// Only arrivals with <c>log_timestamp</c> in the last LookbackDays are (re)filled. Raise it
    /// temporarily to back-fill older arrivals; the vehicle position can only be filled while its
    /// route stop is still kept (see <see cref="RouteStopTraceLogCleanupOptions.RetentionDays"/>).
    /// </summary>
    [Range(1, 365)]
    public int LookbackDays { get; init; } = 7;
}
