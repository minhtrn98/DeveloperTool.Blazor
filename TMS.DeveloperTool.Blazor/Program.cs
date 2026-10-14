using Microsoft.AspNetCore.DataProtection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure environment-specific settings
builder.WebHost.UseStaticWebAssets();

// Configure logging
builder.ConfigureSerilog();

// Persist Data Protection keys so cookies/antiforgery survive app pool recycles on IIS
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

// Add services
builder.Services
    .AddConfigurationSettings(builder.Configuration)
    .AddPresentationServices()
    .AddDatabaseServices()
    .AddCachingServices()
    .AddTmsDatabases()
    .AddRepositories()
    .AddFeatureServices()
    .AddExternalApis()
    .AddBackgroundJobs(builder.Configuration);

WebApplication app = builder.Build();

// Configure middleware and environment
app.ConfigureMiddleware();

await app.RunAsync();
