namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[CommitDeliveryManifest] committed by {Actor}" log row — records who created (committed)
/// a delivery manifest (sổ phát). <see cref="DeliveryManifestCode"/> is the express-delivery
/// manifest code ({DE}), <see cref="CodManifestCode"/> the COD manifest code ({CodCode}).
/// </summary>
public sealed record DeliveryManifestCommitLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string DeliveryManifestCode,
    string CodManifestCode,
    string Actor);
