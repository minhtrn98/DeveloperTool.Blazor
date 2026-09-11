using System.Net.Http.Headers;
using System.Text.Json;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Models;
using TMS.DeveloperTool.Blazor.Infrastructure.Http;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

public sealed class RouteStopLogQueryService(IHttpClientFactory httpClientFactory, LogApiOptions logApiOptions, LogApiTokenProvider tokenProvider)
{
    private const string QueryRangePath = "/api/v5/query_range";
    private const int PageSize = 100;

    private const string MessageTemplateText =
        "{FL} - [ROUTE-STOP] Processing. Action={Action} Driver={Driver} Stop={Office} EventId={EventId}\n{Message} ";

    public async Task<List<RouteStopTraceLogEntry>> QueryAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        List<RouteStopTraceLogEntry> entries = [];
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

    internal static RouteStopTraceLogEntry? ToEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        string action = row.Data.AttributesString.GetValueOrDefault("Action", string.Empty);
        string driver = row.Data.AttributesString.GetValueOrDefault("Driver", string.Empty);
        string office = row.Data.AttributesString.GetValueOrDefault("Office", string.Empty);
        string eventId = row.Data.AttributesString.GetValueOrDefault("EventId", string.Empty);
        string message = row.Data.AttributesString.GetValueOrDefault("Message", string.Empty);
        (string vehicleId, string assignmentId) = ParseMessageIds(message);

        return new RouteStopTraceLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            action,
            driver,
            office,
            eventId,
            vehicleId,
            assignmentId,
            message);
    }

    private static (string VehicleId, string AssignmentId) ParseMessageIds(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (string.Empty, string.Empty);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(message);
            string vehicleId = ReadStringProperty(document.RootElement, "VehicleId");
            string assignmentId = ReadStringProperty(document.RootElement, "AssignmentId");

            return (vehicleId, assignmentId);
        }
        catch (JsonException)
        {
            return (string.Empty, string.Empty);
        }
    }

    private static string ReadStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}
