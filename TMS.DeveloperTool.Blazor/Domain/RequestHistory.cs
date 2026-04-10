using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class RequestHistory
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string JsonBody { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
