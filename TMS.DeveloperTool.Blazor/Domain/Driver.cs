using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class Driver
{
    [Key]
    public Guid DriverId { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public DateTimeOffset? TokenExpiredAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
