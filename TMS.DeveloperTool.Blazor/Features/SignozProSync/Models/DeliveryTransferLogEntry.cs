namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;

/// <summary>
/// One delivery transfer ("chuyển đơn") log row, from either template:
/// <list type="bullet">
/// <item>"[CreateDeliveryTransfer]" — driver → driver (<see cref="SourceType"/> = <see cref="DeliveryTransferSourceType.Driver"/>, {SourceDriverCode}).</item>
/// <item>"[ExternalCreateDeliveryTransfer]" — post-office employee → driver (<see cref="SourceType"/> = <see cref="DeliveryTransferSourceType.Employee"/>, {EmployeeCode}).</item>
/// </list>
/// <see cref="TransferCode"/> is {Code}, <see cref="TargetDriverCode"/> {TargetDriverCode},
/// <see cref="OrderIds"/> the parsed {OrderIds} list, <see cref="TransferredAt"/> {CreatedAt:o}
/// and <see cref="Actor"/> the logged-in user (attributes_string.User).
/// </summary>
public sealed record DeliveryTransferLogEntry(
    string LogId,
    string TraceId,
    string SpanId,
    DateTimeOffset Timestamp,
    string TransferCode,
    string SourceType,
    string SourceCode,
    string TargetDriverCode,
    string[] OrderIds,
    DateTimeOffset? TransferredAt,
    string Actor);

public static class DeliveryTransferSourceType
{
    public const string Driver = "Driver";
    public const string Employee = "Employee";
}
