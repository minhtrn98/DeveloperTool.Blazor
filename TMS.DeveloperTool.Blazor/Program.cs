using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Refit;
using Serilog;
using StackExchange.Redis;
using TMS.DeveloperTool.Blazor.Components;
using TMS.DeveloperTool.Blazor.Features.DriverChange.Services;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;
using TMS.DeveloperTool.Blazor.Features.Pairing.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTask.Services;
using TMS.DeveloperTool.Blazor.Features.Routing.Services;
using TMS.DeveloperTool.Blazor.Features.Simulation.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;
using TMS.DeveloperTool.Blazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopmentOrLocal())
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Bind and validate configuration settings
builder.Services.AddSettingsAndValidate<ConnectionStringsOptions>(builder.Configuration);
builder.Services.AddSettingsAndValidate<MyRedisOptions>(builder.Configuration);
builder.Services.AddSettingsAndValidate<RabbitMqConfig>(builder.Configuration);
builder.Services.AddSettingsAndValidate<JwtOptions>(builder.Configuration);
builder.Services.AddSettingsAndValidate<ApiUrlsOptions>(builder.Configuration);

builder.Services.AddMudServices();

builder.Services.AddHttpClient();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    MyRedisOptions redisOptions = sp.GetRequiredService<MyRedisOptions>();
    ConfigurationOptions configuration = ConfigurationOptions.Parse(redisOptions.ConnectionString, true);
    configuration.Password = redisOptions.Password;

    configuration.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
    configuration.ConnectRetry = redisOptions.ConnectRetry;
    configuration.ConnectTimeout = redisOptions.ConnectTimeout;
    configuration.SyncTimeout = redisOptions.SyncTimeout;
    configuration.AsyncTimeout = redisOptions.AsyncTimeout;
    configuration.DefaultDatabase = redisOptions.Database;

    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    ConnectionStringsOptions connectionStrings = sp.GetRequiredService<ConnectionStringsOptions>();
    options.UseNpgsql(connectionStrings.DeveloperDb, npgsqlOptions =>
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
builder.Services.AddTMSDbQuery("OrderDb");

builder.Services.AddScoped<DriverRepository>();
builder.Services.AddScoped<FleetRepository>();
builder.Services.AddScoped<RouteRepository>();
builder.Services.AddScoped<PlanningRepository>();
builder.Services.AddScoped<OrderRepository>();

builder.Services.AddScoped<FakeVehicleTransportService>();
builder.Services.AddScoped<RouteCheckPointTemplateService>();
builder.Services.AddTransient<LoggingHttpHandler>();
RefitSettings fleetRefitSettings = new(new SystemTextJsonContentSerializer(
    new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    }));
builder.Services.AddRefitClient<IFleetAssignmentApi>(fleetRefitSettings)
    .ConfigureHttpClient((sp, c) =>
    {
        ApiUrlsOptions apiUrls = sp.GetRequiredService<ApiUrlsOptions>();
        c.BaseAddress = new Uri(apiUrls.Fleet);
    })
    .AddHttpMessageHandler<LoggingHttpHandler>();
builder.Services.AddScoped<PairingService>();
builder.Services.AddRefitClient<IPickupTaskApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        ApiUrlsOptions apiUrls = sp.GetRequiredService<ApiUrlsOptions>();
        c.BaseAddress = new Uri(apiUrls.Order);
    })
    .AddHttpMessageHandler<LoggingHttpHandler>();
builder.Services.AddScoped<PickupTaskActionService>();
builder.Services.AddScoped<IJsonTypeMappingStrategy, PickupTaskEventJsonMappingStrategy>();
builder.Services.AddScoped<JsonBuilderService>();

builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<MyDriverService>();

WebApplication app = builder.Build();


// ==================================================


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
