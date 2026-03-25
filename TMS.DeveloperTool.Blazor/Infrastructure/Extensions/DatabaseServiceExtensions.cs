using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TMS.DeveloperTool.Blazor.Features.DriverChange.Services;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds Redis and database services.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
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

        return services;
    }

    /// <summary>
    /// Adds caching and monitoring services.
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
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
}
