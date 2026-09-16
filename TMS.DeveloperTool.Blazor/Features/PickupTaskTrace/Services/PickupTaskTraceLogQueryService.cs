using System.Net.Http.Headers;
using System.Text.Json;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;
using TMS.DeveloperTool.Blazor.Infrastructure.Http;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;

public sealed class PickupTaskTraceLogQueryService(IHttpClientFactory httpClientFactory, LogApiOptions logApiOptions, LogApiTokenProvider tokenProvider)
{
    private const string QueryRangePath = "/api/v5/query_range";
    private const int PageSize = 100;

    private const string MessageTemplateText =
        "{FL} {PT} - Received event, EventId: {EventId}\n{Message}";

    public async Task<List<PickupTaskTraceLogEntry>> QueryAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        List<PickupTaskTraceLogEntry> entries = [];
        int offset = 0;
        string bearerToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);

        while (true)
        {
            LogQueryRangeRequest request = BuildRequest(start, end, offset);
            LogQueryRangeResponse? response = await SendAsync(request, bearerToken, cancellationToken);
            List<LogRow> rows = response?.Data?.Data?.Results.FirstOrDefault()?.Rows ?? [];
            if (rows.Count == 0)
            {
                break;
            }

            entries.AddRange(rows.Select(ToEntry).Where(entry => entry is not null)!);

            if (rows.Count < PageSize)
            {
                break;
            }

            offset += PageSize;
        }

        return entries;
    }

    private async Task<LogQueryRangeResponse?> SendAsync(LogQueryRangeRequest request, string bearerToken, CancellationToken cancellationToken)
    {
        string baseUrl = logApiOptions.BaseUrl.Trim().TrimEnd('/');
        using HttpClient client = httpClientFactory.CreateClient();
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, $"{baseUrl}{QueryRangePath}")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Log API request thất bại: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
        }

        return await response.Content.ReadFromJsonAsync<LogQueryRangeResponse>(cancellationToken);
    }

    private static LogQueryRangeRequest BuildRequest(DateTimeOffset start, DateTimeOffset end, int offset)
    {
        string expression = $"message_template.text = '{MessageTemplateText}'";

        return new LogQueryRangeRequest
        {
            Start = start.ToUnixTimeMilliseconds(),
            End = end.ToUnixTimeMilliseconds(),
            CompositeQuery = new LogCompositeQuery
            {
                Queries =
                [
                    new LogBuilderQuery
                    {
                        Spec = new LogQuerySpec
                        {
                            Filter = new LogFilter { Expression = expression },
                            Limit = PageSize,
                            Offset = offset,
                            Order =
                            [
                                new LogOrder { Key = new LogOrderKey { Name = "timestamp" }, Direction = "desc" },
                                new LogOrder { Key = new LogOrderKey { Name = "id" }, Direction = "desc" }
                            ]
                        }
                    }
                ]
            }
        };
    }

    internal static PickupTaskTraceLogEntry? ToEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        string pickupTaskId = row.Data.AttributesString.GetValueOrDefault("PT", string.Empty);
        string eventId = row.Data.AttributesString.GetValueOrDefault("EventId", string.Empty);
        string message = row.Data.AttributesString.GetValueOrDefault("Message", string.Empty);
        string deliveryLineId = ParseDeliveryLineId(message);

        return new PickupTaskTraceLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            pickupTaskId,
            deliveryLineId,
            eventId,
            message);
    }

    private static string ParseDeliveryLineId(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(message);
            return document.RootElement.TryGetProperty("DeliveryLineID", out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
