namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[RecordDeliveryFailure]" log row — a failed delivery attempt ("giao thất bại").
/// <see cref="DeliveryManifestCode"/> is the manifest code ({DE}), <see cref="RecordId"/> the failure
/// record id, <see cref="FailureType"/> the failure type ({Type}), <see cref="TaskCount"/> /
/// <see cref="ManifestCount"/> / <see cref="ItemCount"/> the number of tasks / manifests / items
/// ({Count}), <see cref="DriverId"/> the driver and <see cref="Actor"/> the user who recorded it
/// (attributes_string.User).
/// </summary>
public sealed record DeliveryFailureLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string DeliveryManifestCode,
    string RecordId,
    string FailureType,
    int TaskCount,
    int ManifestCount,
    int ItemCount,
    string DriverId,
    string Actor);
