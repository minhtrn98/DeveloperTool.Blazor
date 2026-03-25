using TMS.DeveloperTool.Blazor.Components;
using TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure environment-specific settings
if (builder.Environment.IsDevelopmentOrLocal())
{
    builder.WebHost.UseStaticWebAssets();
}

// Configure logging
builder.ConfigureSerilog();

// Add services
builder.Services
    .AddConfigurationSettings(builder.Configuration)
    .AddPresentationServices()
    .AddInfrastructureServices()
    .AddTmsDatabases()
    .AddRepositories()
    .AddFeatureServices()
    .AddExternalApis();


// Configure middleware and environment
WebApplication app = builder.Build();
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

await app.RunAsync();
