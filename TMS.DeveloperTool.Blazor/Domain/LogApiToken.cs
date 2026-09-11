namespace TMS.DeveloperTool.Blazor.Domain;

/// <summary>
/// Single-row table holding the current Log API access/refresh token pair so the app
/// can log in automatically instead of requiring a manually pasted bearer token.
/// </summary>
public sealed class LogApiToken
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset? AccessTokenExpiredAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset? RefreshTokenExpiredAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
