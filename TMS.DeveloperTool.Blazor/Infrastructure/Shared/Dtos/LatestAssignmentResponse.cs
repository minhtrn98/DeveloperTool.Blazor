using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record LatestAssignmentResponse
{
    public required Guid? AssignmentId { get; init; }
    public required decimal? Odometer { get; init; }
    public required string? StartOfficeCode { get; init; }
    public required string? Address { get; init; }
    public required int? CheckStatus { get; init; }
    public required Guid? PlanningId { get; init; }
    public required AssignmentPlanType? PlanningType { get; init; }
}
