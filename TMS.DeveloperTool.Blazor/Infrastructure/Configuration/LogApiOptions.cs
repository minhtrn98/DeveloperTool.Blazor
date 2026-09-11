using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class LogApiOptions
{
    public const string SectionName = "LogApi";

    [Required]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Seed refresh token used to bootstrap Log API access the very first time.
    /// Once a login succeeds, the rotated refresh token is persisted in the Developer DB
    /// and this value is no longer used.
    /// </summary>
    public string? InitialRefreshToken { get; init; }
}
