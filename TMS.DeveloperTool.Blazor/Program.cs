using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using StackExchange.Redis;
using TMS.DeveloperTool.Blazor.Components;
using TMS.DeveloperTool.Blazor.Database;
using TMS.DeveloperTool.Blazor.Services;
using TMS.DeveloperTool.Blazor.SettingModels;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddMudServices();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    string cnnStr = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? throw new Exception("Missing redis connection string!!!");
    ConfigurationOptions configuration = ConfigurationOptions.Parse(cnnStr, true);
    configuration.Password = builder.Configuration.GetValue<string>("Redis:Password");

    configuration.AbortOnConnectFail = builder.Configuration.GetValue<bool>("Redis:AbortOnConnectFail");
    configuration.ConnectRetry = builder.Configuration.GetValue<int>("Redis:ConnectRetry");
    configuration.ConnectTimeout = builder.Configuration.GetValue<int>("Redis:ConnectTimeout");
    configuration.SyncTimeout = builder.Configuration.GetValue<int>("Redis:SyncTimeout");
    configuration.AsyncTimeout = builder.Configuration.GetValue<int>("Redis:AsyncTimeout");
    configuration.DefaultDatabase = builder.Configuration.GetValue<int>("Redis:Database");

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
builder.Services.AddKeyedScoped("DriverDb", (sp, _) =>
{
    return new ApplicationDbQuery(sp.GetRequiredService<IConfiguration>(), "DriverDb");
});
builder.Services.AddKeyedScoped("FleetDb", (sp, _) =>
{
    return new ApplicationDbQuery(sp.GetRequiredService<IConfiguration>(), "FleetDb");
});
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<FleetService>();
builder.Services.AddScoped<FakeVehicleTransportService>();
builder.Services.AddScoped<RouteCheckPointTemplateService>();
builder.Services.AddSingleton<EventService>();

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
