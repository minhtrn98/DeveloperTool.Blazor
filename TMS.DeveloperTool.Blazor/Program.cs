using StackExchange.Redis;
using TMS.DeveloperTool.Blazor.Components;
using TMS.DeveloperTool.Blazor.Database;
using TMS.DeveloperTool.Blazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

builder.Services.AddSingleton<RequestChangeDriverMonitorService>();
builder.Services.AddKeyedSingleton("DriverDb", (sp, _) =>
{
    return new ApplicationDbQuery(sp.GetRequiredService<IConfiguration>(), "DriverDb");
});
builder.Services.AddKeyedSingleton("FleetDb", (sp, _) =>
{
    return new ApplicationDbQuery(sp.GetRequiredService<IConfiguration>(), "FleetDb");
});
builder.Services.AddSingleton<DriverService>();
builder.Services.AddSingleton<FleetService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
