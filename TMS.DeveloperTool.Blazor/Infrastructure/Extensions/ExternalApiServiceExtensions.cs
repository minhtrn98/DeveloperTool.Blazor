using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Http;
using TMS.DeveloperTool.Blazor.Services;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class ExternalApiServiceExtensions
{
    /// <summary>
    /// Adds external API clients (Refit clients).
    /// </summary>
    public static IServiceCollection AddExternalApis(this IServiceCollection services)
    {
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Adds feature services for various business logic.
    /// </summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<MyEmployeeService>();
        services.AddScoped<LogQueryService>();
        services.AddScoped<OrderStep1TraceLogStorageService>();
        services.AddScoped<RouteStopLogQueryService>();
        services.AddScoped<RouteStopTraceLogStorageService>();
        services.AddScoped<PickupTaskTraceLogQueryService>();
        services.AddScoped<PickupTaskTraceLogStorageService>();

        services.AddSingleton<EventService>();
        services.AddSingleton<LogApiTokenProvider>();

        return services;
    }
}
