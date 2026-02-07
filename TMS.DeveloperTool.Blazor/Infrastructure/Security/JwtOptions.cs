namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public List<string> Audiences { get; set; } = [];
    public int DefaultExpiresMinutes { get; set; } = 60;
}
