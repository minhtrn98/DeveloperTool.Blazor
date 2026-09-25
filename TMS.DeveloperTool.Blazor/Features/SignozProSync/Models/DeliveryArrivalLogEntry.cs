namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[RecordDeliveryArrival]" log row — a driver check-in at a delivery point.
/// <see cref="DeliveryManifestCode"/> is {DE}, <see cref="DistanceMeters"/> {Distance} (metres from
/// the delivery address), <see cref="IsGpsValid"/> {IsGpsValid} (null when missing from the log) and
/// <see cref="Superseded"/> {Superseded} (a bool is stored as 0/1, a number as-is) — marks a
/// check-in that was redone. <see cref="Actor"/> is attributes_string.User.
/// </summary>
public sealed record DeliveryArrivalLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string DeliveryManifestCode,
    string ArrivalId,
    string TaskId,
    string OrderId,
    string VehicleId,
    decimal? DistanceMeters,
    bool? IsGpsValid,
    int Superseded,
    string Actor);
