namespace TMS.DeveloperTool.Blazor.Features.Dashboard.Models;

/// <summary>
/// One log row from any synced table, normalized for counting. <see cref="Quantity"/> is the per-type
/// volume (see <see cref="DashboardEventType.QuantityLabel"/>), <see cref="Amount"/> the COD collected
/// (task complete only).
/// </summary>
public sealed record DashboardEvent(string EventType, DateTimeOffset Timestamp, string Actor, int Quantity = 0, decimal Amount = 0);

public sealed record HandoverRecord(DateTimeOffset CreatedAt, DateTimeOffset? ReceivedAt, DateTimeOffset? ConfirmAt, string DriverId, int ItemCount);

public sealed record ArrivalRecord(decimal? DistanceMeters, bool? IsGpsValid, bool Superseded);

/// <summary>A (manifest code, timestamp) pair used to stitch manifest lifecycles together.</summary>
public sealed record ManifestEvent(string ManifestCode, DateTimeOffset Timestamp);

/// <summary>When a manifest was created, first checked in at a stop, and last had a task completed.</summary>
public sealed record ManifestLifecycle(string ManifestCode, DateTimeOffset CommittedAt, DateTimeOffset? FirstArrivalAt, DateTimeOffset? LastCompleteAt);

public sealed record LabelCount(string Label, int Count);

/// <summary>Everything the dashboard needs for one date range, loaded once and re-aggregated in memory.</summary>
public sealed record DashboardSnapshot(
    DateOnly StartDate,
    DateOnly EndDate,
    List<DashboardEvent> Events,
    List<HandoverRecord> Handovers,
    List<ArrivalRecord> Arrivals,
    List<LabelCount> FailureTypes,
    List<LabelCount> TransferSources,
    List<ManifestLifecycle> Manifests);
