using TMS.DeveloperTool.Blazor.Components;

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
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
