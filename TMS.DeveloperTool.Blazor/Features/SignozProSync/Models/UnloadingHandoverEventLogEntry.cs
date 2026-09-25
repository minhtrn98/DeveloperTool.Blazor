namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One NVKT-side unloading handover log row — "[ReceiveUnloadingHandover]" or
/// "[ConfirmUnloadingHandover]". Not stored on its own: it marks the matching
/// <c>pro.unloading_handover_logs</c> row (by <see cref="HandoverCode"/> = {HO}) as received / confirmed.
/// </summary>
public sealed record UnloadingHandoverEventLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string HandoverCode,
    string HandoverId);
