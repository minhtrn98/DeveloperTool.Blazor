using System.Net;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

public sealed partial class LoggingHttpHandler(ILogger<LoggingHttpHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = string.Empty;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        string headers = string.Join("\n", request.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}"));
        LogRequest(
            request.Method,
            request.RequestUri,
            headers,
            body);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // log response
        string responseBody = string.Empty;
        if (response.Content is not null)
        {
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        LogResponse(
            request.Method,
            request.RequestUri,
            response.StatusCode,
            responseBody);

        return response;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = """
            [Refit] Sending request
            {Method} {Url}
            {Headers}

            {Body}
            """)]
    private partial void LogRequest(HttpMethod method, Uri? url, string headers, string body);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Refit] {Method} {Url} responded with {StatusCode}\nBody:\n{Body}")]
    private partial void LogResponse(HttpMethod method, Uri? url, HttpStatusCode statusCode, string body);
}
