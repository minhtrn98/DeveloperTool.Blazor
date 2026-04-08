namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Identity";

    public required string Issuer { get; init; } = string.Empty;
    public required string Audience { get; init; } = string.Empty;
    public required int DefaultExpiresMinutes { get; init; } = 60;
}
