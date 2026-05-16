using Microsoft.Extensions.Options;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

public sealed class QuanLyXeApiKeyHandler(IOptions<QuanLyXeOptions> options) : DelegatingHandler
{
    private readonly QuanLyXeOptions _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UriBuilder uriBuilder = new(request.RequestUri!);
        string apiKeyParam = $"apikey={Uri.EscapeDataString(_options.ApiKey)}";

        uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
            ? apiKeyParam
            : uriBuilder.Query.TrimStart('?') + "&" + apiKeyParam;

        request.RequestUri = uriBuilder.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}
