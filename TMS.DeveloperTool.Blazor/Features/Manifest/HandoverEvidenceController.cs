using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using TMS.DeveloperTool.Blazor.Services;

namespace TMS.DeveloperTool.Blazor.Features.Manifest;

[ApiController]
[Route("api/handover-evidence")]
public class HandoverEvidenceController(
    IHttpClientFactory httpClientFactory,
    ApiUrlsOptions apiUrlsOptions,
    MyEmployeeService employeeService) : ControllerBase
{
    [HttpGet("proxy")]
    public async Task<IActionResult> ProxyAsync([FromQuery] string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path)) return BadRequest();

        string? token = await employeeService.GetLatestBearerTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token)) return Unauthorized();

        string fileUrl = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? path
            : $"{apiUrlsOptions.File.TrimEnd('/')}/api/v1/{path.TrimStart('/')}";

        HttpClient client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await client.GetAsync(fileUrl, cancellationToken);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return File(bytes, contentType);
    }
}
