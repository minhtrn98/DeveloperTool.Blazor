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

        return await base.SendAsync(request, cancellationToken);
    }
}
