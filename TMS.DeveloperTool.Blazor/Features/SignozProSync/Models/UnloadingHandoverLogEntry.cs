namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[CreateUnloadingHandover]" log row — a new unloading handover code (mã bàn giao xuống hàng)
/// created by a driver. <see cref="HandoverCode"/> is {HO}, <see cref="HandoverId"/> {Id},
/// <see cref="DriverId"/> {DriverId}, <see cref="ItemCount"/> {ItemCount} and <see cref="Actor"/>
/// attributes_string.User.
/// </summary>
public sealed record UnloadingHandoverLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string HandoverCode,
    string HandoverId,
    string DriverId,
    int ItemCount,
    string Actor);
