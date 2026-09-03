using System.Net.Http.Headers;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

public sealed class LogQueryService(IHttpClientFactory httpClientFactory, LogApiOptions logApiOptions)
{
    private const string QueryRangePath = "/api/v5/query_range";
    private const int PageSize = 100;

    private const string MessageTemplateText =
        "{AGG} Step (ORD): {At}, order Id: {OrderId}, items: {ItemCount}, serializeMs: {SerializeMs}, messageDetail: {MessageDetail}";

    public async Task<List<OrderStep1TraceLogEntry>> QueryAsync(
        string orderId,
        DateTimeOffset start,
        DateTimeOffset end,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        List<OrderStep1TraceLogEntry> entries = [];
        int offset = 0;

        while (true)
        {
            LogQueryRangeRequest request = BuildRequest(orderId, start, end, offset);
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

    private static LogQueryRangeRequest BuildRequest(string orderId, DateTimeOffset start, DateTimeOffset end, int offset)
    {
        string escapedOrderId = orderId.Replace("'", "''");
        string expression = $"AGG='{escapedOrderId}' AND OrderId='{escapedOrderId}' AND message_template.text = '{MessageTemplateText}'";

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

    internal static OrderStep1TraceLogEntry? ToEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        string messageDetail = row.Data.AttributesString.GetValueOrDefault("MessageDetail", string.Empty);
        string logOrderId = row.Data.AttributesString.GetValueOrDefault("OrderId", string.Empty);

        return new OrderStep1TraceLogEntry(
            row.Data.Id,
            logOrderId,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            messageDetail);
    }
}
