namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// Registers the sync jobs (<c>SignozProSyncJob</c>, <c>PickupTaskOrderSyncJob</c>) when true.
    /// Turn off for a debug session running next to another instance, so the two never sync the
    /// Pro DB / checkpoint file at the same time. Read once at startup.
    /// </summary>
    public bool Enabled { get; init; } = true;
}
