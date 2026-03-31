namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

public sealed class LoggingHttpHandler(ILogger<LoggingHttpHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = string.Empty;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        logger.LogInformation(
            "[Refit] {Method} {Url}\nBody: {Body}",
            request.Method,
            request.RequestUri,
            body);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // log response
        string responseBody = string.Empty;
        if (response.Content is not null)
        {
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        logger.LogInformation(
            "[Refit] {Method} {Url} responded with {StatusCode}\nBody: {Body}",
            request.Method,
            request.RequestUri,
            response.StatusCode,
            responseBody);

        return response;
    }
}
