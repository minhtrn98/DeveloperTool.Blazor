using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Refit;
using StackExchange.Redis;
using TMS.DeveloperTool.Blazor.Features.DriverChange.Services;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;
using TMS.DeveloperTool.Blazor.Features.Pairing.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTask.Services;
using TMS.DeveloperTool.Blazor.Features.Routing.Services;
using TMS.DeveloperTool.Blazor.Features.Simulation.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;
using TMS.DeveloperTool.Blazor.Services;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration settings and validates them.
    /// </summary>
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfigurationManager config)
    {
        services.AddSettingsAndValidate<ConnectionStringsOptions>(config);
        services.AddSettingsAndValidate<MyRedisOptions>(config);
        services.AddSettingsAndValidate<RabbitMqConfig>(config);
        services.AddSettingsAndValidate<JwtOptions>(config);
        services.AddSettingsAndValidate<ApiUrlsOptions>(config);
        return services;
    }

    /// <summary>
    /// Adds UI and component services (MudBlazor, Razor components).
    /// </summary>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddMudServices();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        return services;
    }

    /// <summary>
    /// Adds infrastructure services (Redis, Database, Caching).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Redis Multiplexer
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            MyRedisOptions redisOptions = provider.GetRequiredService<MyRedisOptions>();
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

        // Application DbContext
        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            ConnectionStringsOptions connectionStrings = provider.GetRequiredService<ConnectionStringsOptions>();
            options.UseNpgsql(connectionStrings.DeveloperDb, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5L), null);
            });
        });

        // Cache and monitoring services
        services.AddSingleton<RequestChangeDriverMonitorService>();
        services.AddSingleton<CacheService>();

        return services;
    }

    /// <summary>
    /// Adds TMS database query services.
    /// </summary>
    public static IServiceCollection AddTmsDatabases(this IServiceCollection services)
    {
        services.AddTMSDbQuery("DriverDb");
        services.AddTMSDbQuery("FleetDb");
        services.AddTMSDbQuery("RouteDb");
        services.AddTMSDbQuery("PlanningDb");
        services.AddTMSDbQuery("OrderDb");
        return services;
    }

    /// <summary>
    /// Adds data repository services.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<DriverRepository>();
        services.AddScoped<FleetRepository>();
        services.AddScoped<RouteRepository>();
        services.AddScoped<PlanningRepository>();
        services.AddScoped<OrderRepository>();
        return services;
    }

    /// <summary>
    /// Adds feature services for various business logic.
    /// </summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<FakeVehicleTransportService>();
        services.AddScoped<RouteCheckPointTemplateService>();
        services.AddScoped<PairingService>();
        services.AddScoped<PickupTaskActionService>();
        services.AddScoped<IJsonTypeMappingStrategy, PickupTaskEventJsonMappingStrategy>();
        services.AddScoped<JsonBuilderService>();
        services.AddScoped<MyDriverService>();

        services.AddSingleton<EventService>();
        services.AddSingleton<JwtTokenService>();

        return services;
    }

    /// <summary>
    /// Adds external API clients (Refit clients).
    /// </summary>
    public static IServiceCollection AddExternalApis(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTransient<LoggingHttpHandler>();

        // Fleet API with custom Refit settings
        RefitSettings fleetRefitSettings = new(new SystemTextJsonContentSerializer(
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }));

        services.AddRefitClient<IFleetAssignmentApi>(fleetRefitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                ApiUrlsOptions apiUrls = sp.GetRequiredService<ApiUrlsOptions>();
                c.BaseAddress = new Uri(apiUrls.Fleet);
            })
            .AddHttpMessageHandler<LoggingHttpHandler>();

        // Pickup Task API
        services.AddRefitClient<IPickupTaskApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                ApiUrlsOptions apiUrls = sp.GetRequiredService<ApiUrlsOptions>();
                c.BaseAddress = new Uri(apiUrls.Order);
            })
            .AddHttpMessageHandler<LoggingHttpHandler>();

        return services;
    }
}
