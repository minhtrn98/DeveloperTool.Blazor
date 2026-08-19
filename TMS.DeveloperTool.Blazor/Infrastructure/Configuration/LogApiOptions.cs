using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class LogApiOptions
{
    public const string SectionName = "LogApi";

    [Required]
    public required string BaseUrl { get; init; }
}
