namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>
/// One row per environment holding the current Log API access/refresh token pair, so each
/// deployment (Local/Development/Staging/Production) logs in to its own LogApi endpoint
/// independently — even when they all share the same DeveloperDb — instead of requiring a
/// manually pasted bearer token.
/// </summary>
public sealed class LogApiToken
{
    public string Env { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset? AccessTokenExpiredAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset? RefreshTokenExpiredAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
