namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class BrowserContext
{
    public const string Key = "browser_id";

    public required string BrowserId { get; set; }

    public BrowserContext(IHttpContextAccessor httpContextAccessor)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        // First visit: cookie is in Response (not Request yet) — middleware stored ID in Items
        // Subsequent visits: cookie is in Request
        if (httpContext.Items.TryGetValue(Key, out object? item) && item is string itemId)
        {
            BrowserId = itemId;
        }
        else
        {
            BrowserId = httpContext.Request.Cookies[Key]!;
        }
    }
}