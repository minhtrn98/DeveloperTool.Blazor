using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record DepartmentDto : IDisplaySearchItem
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid CompanyId { get; set; }

    public string DisplayString => $"[{Code}] {Name}";

    public bool Like(string searchTerm)
    {
        return Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               DisplayString == searchTerm;
    }
}
