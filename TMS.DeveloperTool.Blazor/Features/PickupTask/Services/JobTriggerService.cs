using System.Net.Http.Json;
using System.Text.Json;
using TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Services;

public sealed class JobTriggerService(IHttpClientFactory httpClientFactory, ApiUrlsOptions apiUrlsOptions)
{
    private const string JobsApiPath = "/api/v1/operational/maintain/jobs";
    private const string DefaultGroup = "DEFAULT";
    private static readonly JsonSerializerOptions RequestJsonOptions = new() { PropertyNamingPolicy = null };

    public async Task TriggerCarryItemReattributionJobAsync(string pickupTaskId, CancellationToken cancellationToken = default)
    {
        string apiHost = apiUrlsOptions.Order.Trim().TrimEnd('/');
        string requestUrl = $"{apiHost}{JobsApiPath}/CarryItemReattributionJob/trigger?group={DefaultGroup}";

        using HttpClient client = httpClientFactory.CreateClient();
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, requestUrl)
        {
            Content = JsonContent.Create(new { PickupTaskId = pickupTaskId }, options: RequestJsonOptions)
        };

        HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Trigger job thất bại: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
        }
    }
}
