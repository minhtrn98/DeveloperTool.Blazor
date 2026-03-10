using System.Net.Http.Headers;
using System.Net.Http.Json;
using TMS.DeveloperTool.Blazor.Features.Pairing.Models;

namespace TMS.DeveloperTool.Blazor.Features.Pairing.Services;

public sealed class PairingService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PairingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<ApiCallResult> SendPairingAsync(
        string apiHost,
        string apiPath,
        string accessToken,
        string? companyId,
        string? departmentId,
        PairingRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendRequestAsync(apiHost, apiPath, accessToken, companyId, departmentId, request, cancellationToken);
    }

    public Task<ApiCallResult> SendSwapDriverAsync(
        string apiHost,
        string apiPath,
        string accessToken,
        string? companyId,
        string? departmentId,
        SwapDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendRequestAsync(apiHost, apiPath, accessToken, companyId, departmentId, request, cancellationToken);
    }

    private async Task<ApiCallResult> SendRequestAsync<T>(
        string apiHost,
        string apiPath,
        string accessToken,
        string? companyId,
        string? departmentId,
        T request,
        CancellationToken cancellationToken)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, BuildApiUrl(apiHost, apiPath))
        {
            Content = JsonContent.Create(request)
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        }

        AddHeaderIfGuid(httpRequest, "CompanyId", companyId);
        AddHeaderIfGuid(httpRequest, "DepartmentId", departmentId);

        using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return ApiCallResult.Success();
        }

        string error = await response.Content.ReadAsStringAsync(cancellationToken);
        string message = $"{(int)response.StatusCode} {response.ReasonPhrase}. {error}";
        return ApiCallResult.Failure(message);
    }

    private static void AddHeaderIfGuid(HttpRequestMessage httpRequest, string headerName, string? headerValue)
    {
        if (Guid.TryParse(headerValue, out Guid parsed))
        {
            httpRequest.Headers.Add(headerName, parsed.ToString());
        }
    }

    private static string BuildApiUrl(string apiHost, string apiPath)
    {
        if (string.IsNullOrWhiteSpace(apiHost))
        {
            return apiPath;
        }

        string trimmedHost = apiHost.Trim().TrimEnd('/');
        return $"{trimmedHost}{apiPath}";
    }
}

public sealed class ApiCallResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private ApiCallResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static ApiCallResult Success() => new(true, null);

    public static ApiCallResult Failure(string errorMessage) => new(false, errorMessage);
}
