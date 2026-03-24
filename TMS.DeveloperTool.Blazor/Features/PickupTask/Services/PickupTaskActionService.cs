namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Services;

public sealed class PickupTaskActionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiUrlsOptions _apiUrls;

    public PickupTaskActionService(IHttpClientFactory httpClientFactory, ApiUrlsOptions apiUrls)
    {
        _httpClientFactory = httpClientFactory;
        _apiUrls = apiUrls;
    }

    public Task AcceptAsync(string pickupTaskId, string bearerToken, CancellationToken cancellationToken = default)
    {
        string encodedPickupTaskId = Uri.EscapeDataString(pickupTaskId);
        string path = $"/accept?pickupTaskId={encodedPickupTaskId}";
        return SendAsync(HttpMethod.Put, path, body: null, bearerToken, cancellationToken);
    }

    public Task ConfirmArrivedAsync(string pickupTaskId, string bearerToken, CancellationToken cancellationToken = default)
    {
        ConfirmArrivalRequest request = new([pickupTaskId]);
        return SendAsync(HttpMethod.Post, "/confirm-arrived", request, bearerToken, cancellationToken);
    }

    public Task DriverCancelAsync(
        string pickupTaskId,
        string bearerToken,
        string reason,
        string[] orderIds,
        CancellationToken cancellationToken = default)
    {
        DriverCancelPickupTaskRequest request = new(pickupTaskId, reason, orderIds);
        return SendAsync(HttpMethod.Put, "/driver-cancel", request, bearerToken, cancellationToken);
    }

    public Task RescheduleAsync(
        string pickupTaskId,
        string bearerToken,
        DateTime rescheduledPickupDt,
        string reason,
        string[] orderIds,
        CancellationToken cancellationToken = default)
    {
        ReschedulePickupTaskRequest request = new(
            pickupTaskId,
            rescheduledPickupDt.ToUniversalTime(),
            reason,
            orderIds);

        return SendAsync(HttpMethod.Put, "/reschedule", request, bearerToken, cancellationToken);
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, string bearerToken, CancellationToken cancellationToken)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        string baseUrl = _apiUrls.Order.TrimEnd('/');
        string normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        string url = $"{baseUrl}/api/v1/pickup-tasks{normalizedPath}";

        using HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        string error = $"{(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}";
        throw new InvalidOperationException(error);
    }
}

public sealed record DriverCancelPickupTaskRequest(
    string PickupTaskId,
    string Reason,
    string[] OrderIds);

public sealed record ConfirmArrivalRequest(List<string> PickupTaskIds);

public sealed record ReschedulePickupTaskRequest(
    string PickupTaskId,
    DateTime RescheduledPickupDt,
    string Reason,
    string[] OrderIds);
