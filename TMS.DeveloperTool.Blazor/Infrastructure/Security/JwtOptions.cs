namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class JwtOptions
{
    public required string Issuer { get; init; } = string.Empty;
    public required string Key { get; init; } = string.Empty;
    public required string Audience { get; init; } = string.Empty;
    public required int DefaultExpiresMinutes { get; init; } = 60;
    public required string[] Permissions { get; init; }
}
