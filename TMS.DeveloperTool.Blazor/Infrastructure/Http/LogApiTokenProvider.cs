using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

/// <summary>
/// Logs in to the Log API using a stored (rotating) refresh token and hands out a valid
/// access token on demand, so callers never need a manually pasted bearer token.
/// </summary>
public sealed class LogApiTokenProvider(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IHttpClientFactory httpClientFactory,
    LogApiOptions logApiOptions,
    IWebHostEnvironment webHostEnvironment)
{
    private const string LoginPath = "/api/v1/login";
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        string env = webHostEnvironment.EnvironmentName;

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        LogApiToken? token = await dbContext.LogApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Env == env, cancellationToken);

        if (IsAccessTokenValid(token))
        {
            return token!.AccessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            await using ApplicationDbContext lockedDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            token = await lockedDbContext.LogApiTokens
                .FirstOrDefaultAsync(t => t.Env == env, cancellationToken);

            if (IsAccessTokenValid(token))
            {
                return token!.AccessToken;
            }

            string? refreshToken = !string.IsNullOrWhiteSpace(token?.RefreshToken)
                ? token.RefreshToken
                : logApiOptions.InitialRefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidOperationException(
                    "Chưa có Refresh Token cho Log API. Vui lòng cấu hình LogApi:InitialRefreshToken.");
            }

            LogApiLoginResponse loginResponse = await LoginAsync(refreshToken, cancellationToken);

            token ??= new LogApiToken { Env = env };
            token.AccessToken = loginResponse.AccessJwt;
            token.AccessTokenExpiredAt = DateTimeOffset.FromUnixTimeSeconds(loginResponse.AccessJwtExpiry);
            token.RefreshToken = loginResponse.RefreshJwt;
            token.RefreshTokenExpiredAt = DateTimeOffset.FromUnixTimeSeconds(loginResponse.RefreshJwtExpiry);
            token.UpdatedAt = DateTimeOffset.UtcNow;

            if (lockedDbContext.Entry(token).State == EntityState.Detached)
            {
                lockedDbContext.LogApiTokens.Add(token);
            }

            await lockedDbContext.SaveChangesAsync(cancellationToken);

            return token.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsAccessTokenValid(LogApiToken? token)
    {
        return token is not null
            && !string.IsNullOrWhiteSpace(token.AccessToken)
            && token.AccessTokenExpiredAt.HasValue
            && token.AccessTokenExpiredAt.Value - ExpiryBuffer > DateTimeOffset.UtcNow;
    }

    private async Task<LogApiLoginResponse> LoginAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string baseUrl = logApiOptions.BaseUrl.Trim().TrimEnd('/');
        using HttpClient client = httpClientFactory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"{baseUrl}{LoginPath}",
            new LogApiLoginRequest { RefreshToken = refreshToken },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Log API login thất bại: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
        }

        LogApiLoginEnvelope? envelope = await response.Content.ReadFromJsonAsync<LogApiLoginEnvelope>(cancellationToken);
        if (envelope?.Data is null || string.IsNullOrWhiteSpace(envelope.Data.AccessJwt))
        {
            throw new InvalidOperationException("Log API login response không hợp lệ.");
        }

        return envelope.Data;
    }
}

internal sealed class LogApiLoginRequest
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

internal sealed class LogApiLoginEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public LogApiLoginResponse? Data { get; set; }
}

internal sealed class LogApiLoginResponse
{
    [JsonPropertyName("accessJwt")]
    public string AccessJwt { get; set; } = string.Empty;

    [JsonPropertyName("accessJwtExpiry")]
    public long AccessJwtExpiry { get; set; }

    [JsonPropertyName("refreshJwt")]
    public string RefreshJwt { get; set; } = string.Empty;

    [JsonPropertyName("refreshJwtExpiry")]
    public long RefreshJwtExpiry { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}
