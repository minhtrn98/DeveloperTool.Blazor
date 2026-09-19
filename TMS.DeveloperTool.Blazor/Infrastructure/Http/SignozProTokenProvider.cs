using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

/// <summary>
/// Logs in to "SigNoz Pro" (the Production monitor, reached over its own session-rotate API)
/// using a stored (rotating) refresh token, and hands out a valid access token on demand.
/// Tokens are persisted in the "pro" schema under one fixed row, since this is always the same
/// external data source regardless of which environment this tool itself is deployed to.
/// </summary>
public sealed class SignozProTokenProvider(
    IDbContextFactory<ProApplicationDbContext> dbContextFactory,
    IHttpClientFactory httpClientFactory,
    SignozProOptions signozProOptions)
{
    private const string RotatePath = "/api/v2/sessions/rotate";
    private const string TokenEnv = "Production";
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await using ProApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        LogApiToken? token = await dbContext.LogApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Env == TokenEnv, cancellationToken);

        if (IsAccessTokenValid(token))
        {
            return token!.AccessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            await using ProApplicationDbContext lockedDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            token = await lockedDbContext.LogApiTokens
                .FirstOrDefaultAsync(t => t.Env == TokenEnv, cancellationToken);

            if (IsAccessTokenValid(token))
            {
                return token!.AccessToken;
            }

            string? refreshToken = !string.IsNullOrWhiteSpace(token?.RefreshToken)
                ? token.RefreshToken
                : signozProOptions.InitialRefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidOperationException(
                    "Chưa có Refresh Token cho SigNoz Pro. Vui lòng cấu hình SignozPro:InitialRefreshToken.");
            }

            string? previousAccessToken = !string.IsNullOrWhiteSpace(token?.AccessToken)
                ? token.AccessToken
                : signozProOptions.InitialAccessToken;

            SignozProRotateResponse rotated = await RotateAsync(previousAccessToken, refreshToken, cancellationToken);

            token ??= new LogApiToken { Env = TokenEnv };
            token.AccessToken = rotated.AccessToken;
            token.AccessTokenExpiredAt = DateTimeOffset.UtcNow.AddSeconds(rotated.ExpiresIn);
            token.RefreshToken = rotated.RefreshToken;
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

    private async Task<SignozProRotateResponse> RotateAsync(string? previousAccessToken, string refreshToken, CancellationToken cancellationToken)
    {
        string baseUrl = signozProOptions.BaseUrl.Trim().TrimEnd('/');
        using HttpClient client = httpClientFactory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, $"{baseUrl}{RotatePath}")
        {
            Content = JsonContent.Create(new SignozProRotateRequest { RefreshToken = refreshToken })
        };
        if (!string.IsNullOrWhiteSpace(previousAccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", previousAccessToken);
        }

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"SigNoz Pro rotate thất bại: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
        }

        SignozProRotateEnvelope? envelope = await response.Content.ReadFromJsonAsync<SignozProRotateEnvelope>(cancellationToken);
        if (envelope?.Data is null || string.IsNullOrWhiteSpace(envelope.Data.AccessToken))
        {
            throw new InvalidOperationException("SigNoz Pro rotate response không hợp lệ.");
        }

        return envelope.Data;
    }
}

internal sealed class SignozProRotateRequest
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

internal sealed class SignozProRotateEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public SignozProRotateResponse? Data { get; set; }
}

internal sealed class SignozProRotateResponse
{
    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public long ExpiresIn { get; set; }
}
