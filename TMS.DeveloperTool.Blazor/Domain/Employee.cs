using System.ComponentModel.DataAnnotations;
using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class Employee : IDisplaySearchItem
{
    [Key]
    public Guid EmployeeId { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public DateTimeOffset? TokenExpiredAt { get; set; }
    public string Code { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string DisplayString => $"{Code} - {Name}";

    public bool Like(string searchTerm)
    {
        return Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }
}
