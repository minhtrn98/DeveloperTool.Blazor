using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds database services.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        // Application DbContext — registered via factory (not AddDbContext) so every
        // consumer creates its own short-lived context instance per operation instead of
        // sharing one scoped instance for the whole Blazor Server circuit. A single scoped
        // DbContext is not thread-safe/reentrant-safe, and Blazor's rendering pipeline can
        // easily run two async DB operations concurrently on the same circuit (e.g. a
        // page's OnInitializedAsync racing with a child MudTable's own initial ServerData
        // call), which throws "A second operation was started on this context instance
        // before a previous operation completed."
        services.AddDbContextFactory<ApplicationDbContext>((provider, options) =>
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
        services.AddScoped<BrowserContext>();
        return services;
    }

    /// <summary>
    /// Adds TMS database query services.
    /// </summary>
    public static IServiceCollection AddTmsDatabases(this IServiceCollection services)
    {
        services.AddTMSDbQuery("FleetDb");
        services.AddTMSDbQuery("OrderDb");
        return services;
    }

    /// <summary>
    /// Adds data repository services.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<FleetRepository>();
        services.AddScoped<OrderRepository>();
        return services;
    }
}
