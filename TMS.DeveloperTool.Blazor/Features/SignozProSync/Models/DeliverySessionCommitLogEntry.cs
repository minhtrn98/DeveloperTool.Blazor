namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One "[CommitCreateDeliverySession]" log row — manifests (sổ phát) created by scanning a
/// pre-created handover code. <see cref="ManifestCount"/> is {ManifestCount},
/// <see cref="ManifestCodes"/> the parsed {Codes} list, <see cref="ItemCount"/> {Count} and
/// <see cref="Actor"/> attributes_string.User (fallback {Actor}).
/// </summary>
public sealed record DeliverySessionCommitLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    int ManifestCount,
    string[] ManifestCodes,
    int ItemCount,
    string Actor);
