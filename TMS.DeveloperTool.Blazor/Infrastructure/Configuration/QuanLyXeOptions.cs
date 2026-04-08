using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class QuanLyXeOptions
{
    public const string SectionName = "QuanLyXe";

    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public required string ApiKey { get; init; }
}
