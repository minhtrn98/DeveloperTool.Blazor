using TMS.DeveloperTool.Blazor.Components;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures common middleware and routing.
    /// </summary>
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsProduction())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        if (app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }
        app.UseAntiforgery();
        app.MapStaticAssets();

        app.Use(async (context, next) =>
        {
            if (!context.Request.Cookies.ContainsKey(BrowserContext.Key))
            {
                string newId = Guid.NewGuid().ToString();
                context.Response.Cookies.Append(BrowserContext.Key, newId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    SameSite = SameSiteMode.Lax
                });
                // Cookie is in Response, not yet in Request — store in Items for this request
                context.Items[BrowserContext.Key] = newId;
            }

            await next();
        });

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapControllers();

        return app;
    }
}
