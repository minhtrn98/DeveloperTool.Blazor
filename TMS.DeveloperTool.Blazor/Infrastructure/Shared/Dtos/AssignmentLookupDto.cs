using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record AssignmentLookupDto(Guid Id, string Code) : IDisplaySearchItem
{
    public string DisplayString => Code;

    public bool Like(string searchTerm) => Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
