namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[CompleteDeliveryTask]" log row — the "xác nhận phát hàng" action.
/// <see cref="DeliveryManifestCode"/> is the manifest code ({DE}), <see cref="TaskCount"/> the
/// number of tasks, <see cref="ManifestCount"/> the number of manifests, <see cref="DeliveredCount"/>
/// the number of parcels delivered and <see cref="CollectedCod"/> the COD amount collected ({Cod}).
/// </summary>
public sealed record DeliveryTaskCompleteLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string DeliveryManifestCode,
    int TaskCount,
    int ManifestCount,
    int DeliveredCount,
    decimal CollectedCod);
