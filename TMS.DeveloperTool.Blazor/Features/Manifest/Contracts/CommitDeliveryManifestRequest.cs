using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

public sealed class CommitDeliveryManifestRequest
{
    public required DeliveryMode DeliveryMode { get; init; }
    public Guid? HandoverConditionId { get; init; }
    public required List<CommitDeliveryOrderInput> Orders { get; init; }
    public List<CommitDeliveryEvidenceInput> ReceiverEvidences { get; init; } = [];
    public required List<CommitDeliveryParticipantInput> Participants { get; init; }
}

public sealed class CommitDeliveryOrderInput
{
    public required string OrderId { get; init; }
    public required OrderType OrderType { get; init; }
    public List<CommitDeliveryItemInput> Items { get; init; } = [];
}

public sealed class CommitDeliveryItemInput
{
    public required string OrderItemId { get; init; }
    public required bool IsLoaded { get; init; }
}

public sealed class CommitDeliveryEvidenceInput
{
    public required EvidenceType EvidenceType { get; init; }
    public Guid? FileId { get; init; }
    public required string FileUrl { get; init; }
    public string? FileName { get; init; }
}

public sealed class CommitDeliveryParticipantInput
{
    public required Guid EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public required string EmployeeName { get; init; }
    public string? EmployeeAvatarUrl { get; init; }
    public bool IsResponsible { get; init; } = true;
    public decimal ParticipationRate { get; init; } = 100m;
}
