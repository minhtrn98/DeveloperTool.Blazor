using System.Globalization;
using System.Net.Http.Headers;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Models;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;
using TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;
using TMS.DeveloperTool.Blazor.Infrastructure.Http;

namespace TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;

/// <summary>
/// Queries SigNoz Pro (the Production monitor) for the same trace message templates already
/// tracked locally. RouteStop/PickupTask pull every matching row in a window (no entity-id
/// filter, used by <see cref="SignozProSyncJob"/> for periodic bulk sync); OrderStep1 is instead
/// looked up one order id at a time (used by the "Order Step 1" Pro trace page), mirroring
/// <see cref="LogQueryService.QueryAsync"/> — SigNoz has too many OrderStep1 events to poll in
/// bulk without an id filter.
/// </summary>
public sealed class SignozProQueryService(
    IHttpClientFactory httpClientFactory,
    SignozProOptions signozProOptions,
    SignozProTokenProvider tokenProvider,
    ILogger<SignozProQueryService> logger)
{
    private const string QueryRangePath = "/api/v5/query_range";
    private const int PageSize = 100;

    private const int MaxTimeoutRetries = 3;
    private static readonly TimeSpan TimeoutRetryDelay = TimeSpan.FromSeconds(30);

    private const string OrderStep1MessageTemplateText =
        "{AGG} Step (ORD): {At}, order Id: {OrderId}, items: {ItemCount}, serializeMs: {SerializeMs}, messageDetail: {MessageDetail}";

    private const string RouteStopMessageTemplateText =
        "{FL} - [ROUTE-STOP] Processing. Action={Action} Driver={Driver} Stop={Office} EventId={EventId}\n{Message} ";

    private const string PickupTaskMessageTemplateText =
        "{FL} {PT} - Received event, EventId: {EventId}\n{Message}";

    private const string DeliveryManifestCommitMessageTemplateText =
        "{FL} {DE}|{CodCode} - [CommitDeliveryManifest] committed by {Actor}";

    public Task<int> QueryDeliveryManifestCommitAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliveryManifestCommitLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{DeliveryManifestCommitMessageTemplateText}'", ToDeliveryManifestCommitEntry, start, end, onPageAsync, cancellationToken);

    private const string DeliveryTaskCompleteMessageTemplateText =
        "{FL} {DE} - [CompleteDeliveryTask] tasks={TaskCount} manifests={ManifestCount} delivered={Delivered} collected={Cod}";

    public Task<int> QueryDeliveryTaskCompleteAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliveryTaskCompleteLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{DeliveryTaskCompleteMessageTemplateText}'", ToDeliveryTaskCompleteEntry, start, end, onPageAsync, cancellationToken);

    private const string DeliveryFailureMessageTemplateText =
        "{FL} {DE} - [RecordDeliveryFailure] record={RecordId} type={Type} tasks={TaskCount} manifests={ManifestCount} items={Count} driver={DriverId}";

    public Task<int> QueryDeliveryFailureAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliveryFailureLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{DeliveryFailureMessageTemplateText}'", ToDeliveryFailureEntry, start, end, onPageAsync, cancellationToken);

    private const string DriverDeliveryTransferMessageTemplateText =
        "{FL} - [CreateDeliveryTransfer] code={Code} driver {SourceDriverCode} → {TargetDriverCode}, orders=[{OrderIds}], at {CreatedAt:o}";

    private const string EmployeeDeliveryTransferMessageTemplateText =
        "{FL} - [ExternalCreateDeliveryTransfer] code={Code} employee {EmployeeCode} → {TargetDriverCode}, orders=[{OrderIds}], at {CreatedAt:o}";

    /// <summary>Driver → driver transfers; mapped with <see cref="DeliveryTransferSourceType.Driver"/>.</summary>
    public Task<int> QueryDriverDeliveryTransferAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliveryTransferLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{DriverDeliveryTransferMessageTemplateText}'",
            row => ToDeliveryTransferEntry(row, DeliveryTransferSourceType.Driver, "SourceDriverCode"), start, end, onPageAsync, cancellationToken);

    /// <summary>Post-office employee → driver transfers; mapped with <see cref="DeliveryTransferSourceType.Employee"/>.</summary>
    public Task<int> QueryEmployeeDeliveryTransferAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliveryTransferLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{EmployeeDeliveryTransferMessageTemplateText}'",
            row => ToDeliveryTransferEntry(row, DeliveryTransferSourceType.Employee, "EmployeeCode"), start, end, onPageAsync, cancellationToken);

    private const string DeliveryArrivalMessageTemplateText =
        "{FL} {DE} - [RecordDeliveryArrival] arrival={ArrivalId} task={TaskId} order={OrderId} vehicle={VehicleId} distance={Distance}m gpsValid={IsGpsValid} superseded={Superseded}";

    public Task<int> QueryDeliveryArrivalAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliveryArrivalLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{DeliveryArrivalMessageTemplateText}'", ToDeliveryArrivalEntry, start, end, onPageAsync, cancellationToken);

    private const string DeliverySessionCommitMessageTemplateText =
        "{FL} - [CommitCreateDeliverySession] created {ManifestCount} manifest(s) [{Codes}] from pre-created handover; items={Count}, actor={Actor}";

    public Task<int> QueryDeliverySessionCommitAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<DeliverySessionCommitLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{DeliverySessionCommitMessageTemplateText}'", ToDeliverySessionCommitEntry, start, end, onPageAsync, cancellationToken);

    private const string UnloadingHandoverMessageTemplateText =
        "{FL} {HO} - [CreateUnloadingHandover] created (id={Id}) by driver {DriverId} with {ItemCount} items";

    public Task<int> QueryUnloadingHandoverAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<UnloadingHandoverLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{UnloadingHandoverMessageTemplateText}'", ToUnloadingHandoverEntry, start, end, onPageAsync, cancellationToken);

    private const string UnloadingHandoverReceiveMessageTemplateText =
        "{FL} {HO} - [ReceiveUnloadingHandover] NVKT received (id={Id})";

    public Task<int> QueryUnloadingHandoverReceiveAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<UnloadingHandoverEventLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{UnloadingHandoverReceiveMessageTemplateText}'", ToUnloadingHandoverEventEntry, start, end, onPageAsync, cancellationToken);

    private const string UnloadingHandoverConfirmMessageTemplateText =
        "{FL} {HO} - [ConfirmUnloadingHandover] NVKT received with item commit (id={Id})";

    public Task<int> QueryUnloadingHandoverConfirmAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<UnloadingHandoverEventLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{UnloadingHandoverConfirmMessageTemplateText}'", ToUnloadingHandoverEventEntry, start, end, onPageAsync, cancellationToken);

    public Task<int> QueryRouteStopAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<RouteStopTraceLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{RouteStopMessageTemplateText}'", RouteStopLogQueryService.ToEntry, start, end, onPageAsync, cancellationToken);

    public Task<int> QueryPickupTaskAsync(
        DateTimeOffset start, DateTimeOffset end, Func<List<PickupTaskTraceLogEntry>, CancellationToken, Task> onPageAsync, CancellationToken cancellationToken)
        => QueryAsync($"message_template.text = '{PickupTaskMessageTemplateText}'", PickupTaskTraceLogQueryService.ToEntry, start, end, onPageAsync, cancellationToken);

    public async Task<List<OrderStep1TraceLogEntry>> QueryOrderStep1ByOrderIdAsync(
        string orderId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        string escapedOrderId = orderId.Replace("'", "''");
        string expression = $"AGG='{escapedOrderId}' AND OrderId='{escapedOrderId}' AND message_template.text = '{OrderStep1MessageTemplateText}'";

        List<OrderStep1TraceLogEntry> entries = [];
        await QueryAsync(expression, LogQueryService.ToEntry, start, end, (page, _) =>
        {
            entries.AddRange(page);
            return Task.CompletedTask;
        }, cancellationToken);

        return entries;
    }

    /// <summary>
    /// Pages through every log row matching <paramref name="filterExpression"/> in the given
    /// window, invoking <paramref name="onPageAsync"/> once per SigNoz request (up to
    /// <see cref="PageSize"/> rows) instead of accumulating the whole window in memory — the
    /// caller decides how each page is persisted and when to clear tracking.
    /// </summary>
    private async Task<int> QueryAsync<T>(
        string filterExpression,
        Func<LogRow, T?> toEntry,
        DateTimeOffset start,
        DateTimeOffset end,
        Func<List<T>, CancellationToken, Task> onPageAsync,
        CancellationToken cancellationToken)
        where T : class
    {
        int offset = 0;
        int totalCount = 0;

        while (true)
        {
            LogQueryRangeRequest request = BuildRequest(filterExpression, start, end, offset);
            LogQueryRangeResponse? response = await SendAsync(request, cancellationToken);
            List<LogRow> rows = response?.Data?.Data?.Results.FirstOrDefault()?.Rows ?? [];
            if (rows.Count == 0)
            {
                break;
            }

            List<T> page = [.. rows.Select(toEntry).Where(entry => entry is not null)!];
            totalCount += page.Count;

            await onPageAsync(page, cancellationToken);

            if (rows.Count < PageSize)
            {
                break;
            }

            offset += PageSize;

            int minDelayMs = signozProOptions.RequestDelayMinMs;
            int maxDelayMs = signozProOptions.RequestDelayMaxMs;
            if (maxDelayMs > minDelayMs && minDelayMs >= 0)
            {
                await Task.Delay(Random.Shared.Next(minDelayMs, maxDelayMs), cancellationToken);
            }
            else if (minDelayMs > 0)
            {
                await Task.Delay(minDelayMs, cancellationToken);
            }
        }

        return totalCount;
    }

    private async Task<LogQueryRangeResponse?> SendAsync(LogQueryRangeRequest request, CancellationToken cancellationToken)
    {
        string baseUrl = signozProOptions.BaseUrl.Trim().TrimEnd('/');
        string bearerToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        bool hasForcedRotation = false;

        for (int attempt = 1; ; attempt++)
        {
            using HttpClient client = httpClientFactory.CreateClient(SignozProOptions.HttpClientName);
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, $"{baseUrl}{QueryRangePath}")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            try
            {
                using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"SigNoz Pro request thất bại: {(int)response.StatusCode} {response.ReasonPhrase}. {error}");
                }

                return await response.Content.ReadFromJsonAsync<LogQueryRangeResponse>(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxTimeoutRetries)
            {
                // The client's own timeout fired (not the caller's cancellationToken) — sleep
                // briefly and retry instead of failing the whole sync/trace on one blip.
                logger.LogWarning(
                    "SigNoz Pro request timed out (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}.",
                    attempt, MaxTimeoutRetries, TimeoutRetryDelay);
                await Task.Delay(TimeoutRetryDelay, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested && attempt >= MaxTimeoutRetries && !hasForcedRotation)
            {
                // Ran out of retries and it's still failing (timeout again, or a non-2xx like
                // 401) — the token might have been revoked/rotated server-side without our
                // local expiry tracking noticing. Force a fresh token and try once more before
                // finally giving up.
                logger.LogWarning(ex,
                    "SigNoz Pro request still failing after {MaxAttempts} attempts. Forcing a token rotation and retrying once more.",
                    MaxTimeoutRetries);
                hasForcedRotation = true;
                bearerToken = await tokenProvider.GetAccessTokenAsync(forceRefresh: true, cancellationToken);
            }
        }
    }

    // {FL} is deliberately ignored — only the manifest codes and the actor (see GetActor) are kept.
    internal static DeliveryManifestCommitLogEntry? ToDeliveryManifestCommitEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        return new DeliveryManifestCommitLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            row.Data.AttributesString.GetValueOrDefault("DE", string.Empty),
            row.Data.AttributesString.GetValueOrDefault("CodCode", string.Empty),
            GetActor(row.Data));
    }

    // {FL} is deliberately ignored.
    internal static DeliveryTaskCompleteLogEntry? ToDeliveryTaskCompleteEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        return new DeliveryTaskCompleteLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            row.Data.AttributesString.GetValueOrDefault("DE", string.Empty),
            (int)GetNumberAttribute(row.Data, "TaskCount"),
            (int)GetNumberAttribute(row.Data, "ManifestCount"),
            (int)GetNumberAttribute(row.Data, "Delivered"),
            GetNumberAttribute(row.Data, "Cod"),
            GetActor(row.Data));
    }

    // {FL} is deliberately ignored.
    internal static DeliveryFailureLogEntry? ToDeliveryFailureEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        return new DeliveryFailureLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            row.Data.AttributesString.GetValueOrDefault("DE", string.Empty),
            GetTextAttribute(row.Data, "RecordId"),
            GetTextAttribute(row.Data, "Type"),
            (int)GetNumberAttribute(row.Data, "TaskCount"),
            (int)GetNumberAttribute(row.Data, "ManifestCount"),
            (int)GetNumberAttribute(row.Data, "Count"),
            GetTextAttribute(row.Data, "DriverId"),
            GetActor(row.Data));
    }

    // {FL} is deliberately ignored. Both transfer templates share this mapper; only the source
    // attribute ({SourceDriverCode} vs {EmployeeCode}) and the stored source_type differ.
    internal static DeliveryTransferLogEntry? ToDeliveryTransferEntry(LogRow row, string sourceType, string sourceCodeKey)
    {
        if (row.Data is null)
        {
            return null;
        }

        string createdAtText = GetTextAttribute(row.Data, "CreatedAt");
        DateTimeOffset? transferredAt = DateTimeOffset.TryParse(createdAtText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed)
            ? parsed
            : null;

        return new DeliveryTransferLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            GetTextAttribute(row.Data, "Code"),
            sourceType,
            GetTextAttribute(row.Data, sourceCodeKey),
            GetTextAttribute(row.Data, "TargetDriverCode"),
            ParseIdList(GetTextAttribute(row.Data, "OrderIds")),
            transferredAt,
            GetActor(row.Data));
    }

    // List arguments ({OrderIds}, {Codes}) arrive as a single attribute — either "a, b, c" or a
    // serialized array like ["a","b"] depending on how the collection was destructured — so strip
    // brackets/quotes and split.
    private static string[] ParseIdList(string raw)
        => raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => id.Trim('[', ']', '"', '\'', ' '))
            .Where(id => id.Length > 0)
            .Distinct()
            .ToArray();

    // {FL} is deliberately ignored.
    internal static DeliveryArrivalLogEntry? ToDeliveryArrivalEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        bool hasDistance = row.Data.AttributesNumber.ContainsKey("Distance") || row.Data.AttributesString.ContainsKey("Distance");
        bool? superseded = GetBoolAttribute(row.Data, "Superseded");

        return new DeliveryArrivalLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            row.Data.AttributesString.GetValueOrDefault("DE", string.Empty),
            GetTextAttribute(row.Data, "ArrivalId"),
            GetTextAttribute(row.Data, "TaskId"),
            GetTextAttribute(row.Data, "OrderId"),
            GetTextAttribute(row.Data, "VehicleId"),
            hasDistance ? GetNumberAttribute(row.Data, "Distance") : null,
            GetBoolAttribute(row.Data, "IsGpsValid"),
            superseded switch
            {
                true => 1,
                false => 0,
                null => (int)GetNumberAttribute(row.Data, "Superseded")
            },
            GetActor(row.Data));
    }

    // {FL} is deliberately ignored.
    internal static DeliverySessionCommitLogEntry? ToDeliverySessionCommitEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        return new DeliverySessionCommitLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            (int)GetNumberAttribute(row.Data, "ManifestCount"),
            ParseIdList(GetTextAttribute(row.Data, "Codes")),
            (int)GetNumberAttribute(row.Data, "Count"),
            GetActor(row.Data));
    }

    // {FL} is deliberately ignored.
    internal static UnloadingHandoverLogEntry? ToUnloadingHandoverEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        return new UnloadingHandoverLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            GetTextAttribute(row.Data, "HO"),
            GetTextAttribute(row.Data, "Id"),
            GetTextAttribute(row.Data, "DriverId"),
            (int)GetNumberAttribute(row.Data, "ItemCount"),
            GetActor(row.Data));
    }

    // {FL} is deliberately ignored.
    internal static UnloadingHandoverEventLogEntry? ToUnloadingHandoverEventEntry(LogRow row)
    {
        if (row.Data is null)
        {
            return null;
        }

        return new UnloadingHandoverEventLogEntry(
            row.Data.Id,
            row.Data.TraceId,
            row.Data.SpanId,
            row.Timestamp,
            GetTextAttribute(row.Data, "HO"),
            GetTextAttribute(row.Data, "Id"));
    }

    // Bools land in attributes_bool, but may also come through as "True"/"false" strings or 0/1
    // numbers depending on the exporter. Null means the attribute is absent (or not a bool), so a
    // missing flag isn't silently counted as false.
    private static bool? GetBoolAttribute(LogRowData data, string key)
    {
        if (data.AttributesBool.TryGetValue(key, out bool value))
        {
            return value;
        }

        if (data.AttributesString.TryGetValue(key, out string? text) && bool.TryParse(text, out bool parsed))
        {
            return parsed;
        }

        return null;
    }

    // Ids/enums may be logged as numbers, which SigNoz indexes under attributes_number instead
    // of attributes_string — read either so the stored value isn't silently empty.
    private static string GetTextAttribute(LogRowData data, string key)
    {
        if (data.AttributesString.TryGetValue(key, out string? text))
        {
            return text;
        }

        return data.AttributesNumber.TryGetValue(key, out double number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    // The logged-in user is enriched onto every log as attributes_string.User; the commit
    // template's own {Actor} argument is only a fallback for rows missing that attribute.
    private static string GetActor(LogRowData data)
        => data.AttributesString.GetValueOrDefault("User")
            ?? data.AttributesString.GetValueOrDefault("Actor", string.Empty);

    // Numeric template arguments usually land in attributes_number, but fall back to
    // attributes_string in case the exporter stringified them.
    private static decimal GetNumberAttribute(LogRowData data, string key)
    {
        if (data.AttributesNumber.TryGetValue(key, out double number))
        {
            return (decimal)number;
        }

        return data.AttributesString.TryGetValue(key, out string? text)
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : 0m;
    }

    private static LogQueryRangeRequest BuildRequest(string filterExpression, DateTimeOffset start, DateTimeOffset end, int offset)
    {
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
                            Filter = new LogFilter { Expression = filterExpression },
                            Limit = PageSize,
                            Offset = offset,
                            Order =
                            [
                                new LogOrder { Key = new LogOrderKey { Name = "timestamp" }, Direction = "asc" },
                                new LogOrder { Key = new LogOrderKey { Name = "id" }, Direction = "asc" }
                            ]
                        }
                    }
                ]
            }
        };
    }
}
