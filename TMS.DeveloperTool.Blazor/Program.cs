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
    .AddDatabaseServices()
    .AddCachingServices()
    .AddTmsDatabases()
    .AddRepositories()
    .AddFeatureServices()
    .AddExternalApis();

WebApplication app = builder.Build();

// Configure middleware and environment
app.ConfigureMiddleware();

await app.RunAsync();
