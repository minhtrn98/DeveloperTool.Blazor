using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record DailyPlanDto : IDisplaySearchItem
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string DepartmentCode { get; init; }
    public required DateOnly ExecutionDate { get; init; }
    public required string Status { get; init; }

    public List<DailyPlanDetailDto> Details { get; set; } = [];

    public string GetDisplayString()
    {
        return $"{Code} - {Name} ({Status})";
    }

    public bool Like(string searchTerm)
    {
        return Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record DailyPlanDetailDto
{
    public required Guid Id { get; init; }
    public required Guid DailyPlanId { get; init; }
    public required string PostOfficeCode { get; init; }
    public required TimeOnly FromTime { get; init; }
    public required TimeOnly ToTime { get; init; }
    public required int StepNumber { get; init; }
    public required BusinessOperation BusinessOperation { get; init; }
}