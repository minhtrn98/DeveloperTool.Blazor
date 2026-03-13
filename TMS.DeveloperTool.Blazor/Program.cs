using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using StackExchange.Redis;
using TMS.DeveloperTool.Blazor.Components;
using TMS.DeveloperTool.Blazor.Features.DriverChange.Services;
using TMS.DeveloperTool.Blazor.Features.Pairing.Services;
using TMS.DeveloperTool.Blazor.Features.Routing.Services;
using TMS.DeveloperTool.Blazor.Features.Simulation.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;
using TMS.DeveloperTool.Blazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("MyRabbitMq"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Identity"));
builder.Services.Configure<ApiUrlsOptions>(builder.Configuration.GetSection(ApiUrlsOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiUrlsOptions>>().Value);

builder.Services.AddMudServices();

builder.Services.AddHttpClient();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    string cnnStr = builder.Configuration.GetValue<string>("MyRedis:ConnectionString") ?? throw new Exception("Missing redis connection string!!!");
    ConfigurationOptions configuration = ConfigurationOptions.Parse(cnnStr, true);
    configuration.Password = builder.Configuration.GetValue<string>("MyRedis:Password");

    configuration.AbortOnConnectFail = builder.Configuration.GetValue<bool>("MyRedis:AbortOnConnectFail");
    configuration.ConnectRetry = builder.Configuration.GetValue<int>("MyRedis:ConnectRetry");
    configuration.ConnectTimeout = builder.Configuration.GetValue<int>("MyRedis:ConnectTimeout");
    configuration.SyncTimeout = builder.Configuration.GetValue<int>("MyRedis:SyncTimeout");
    configuration.AsyncTimeout = builder.Configuration.GetValue<int>("MyRedis:AsyncTimeout");
    configuration.DefaultDatabase = builder.Configuration.GetValue<int>("MyRedis:Database");

    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DeveloperDb"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5L), null);
    });
});

builder.Services.AddSingleton<RequestChangeDriverMonitorService>();
builder.Services.AddSingleton<CacheService>();

builder.Services.AddTMSDbQuery("DriverDb");
builder.Services.AddTMSDbQuery("FleetDb");
builder.Services.AddTMSDbQuery("RouteDb");
builder.Services.AddTMSDbQuery("PlanningDb");

builder.Services.AddScoped<DriverRepository>();
builder.Services.AddScoped<FleetRepository>();
builder.Services.AddScoped<RouteRepository>();
builder.Services.AddScoped<PlanningRepository>();

builder.Services.AddScoped<FakeVehicleTransportService>();
builder.Services.AddScoped<RouteCheckPointTemplateService>();
builder.Services.AddScoped<PairingService>();

builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<MyDriverService>();

WebApplication app = builder.Build();


// ==================================================


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

static class Extensions
{
    public static IServiceCollection AddTMSDbQuery(this IServiceCollection services, string database)
    {
        services.AddKeyedScoped(database, (sp, _) =>
        {
            return new ApplicationDbQuery(sp.GetRequiredService<IConfiguration>(), database);
        });
        return services;
    }
}