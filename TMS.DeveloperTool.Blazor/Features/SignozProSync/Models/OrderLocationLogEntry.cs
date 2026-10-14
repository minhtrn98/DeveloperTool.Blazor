namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[ORDER-LOCATION] Upserted" log row — the configured delivery location of an order.
/// <see cref="Lat"/>/<see cref="Lng"/> are {Lat}/{Long} parsed from the body (full precision),
/// falling back to attributes_number; null when neither is present. <see cref="Month"/> is
/// {Month}, <see cref="Source"/> {Source} (e.g. "PMS").
/// </summary>
public sealed record OrderLocationLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string OrderId,
    DateOnly? Month,
    decimal? Lat,
    decimal? Lng,
    string Source);
