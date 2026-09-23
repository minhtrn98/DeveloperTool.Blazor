using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskOrderSync.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;
using TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Http;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class ExternalApiServiceExtensions
{
    /// <summary>
    /// Adds external API clients (Refit clients).
    /// </summary>
    public static IServiceCollection AddExternalApis(this IServiceCollection services)
    {
        services.AddHttpClient();

        // Short timeout so a stuck/slow SigNoz Pro request fails fast — callers retry with a
        // short sleep on timeout (see SignozProQueryService/SignozProTokenProvider) instead of
        // blocking a BackgroundService tick for the default 100s.
        services.AddHttpClient(SignozProOptions.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));

        return services;
    }

    /// <summary>
    /// Adds feature services for various business logic.
    /// </summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<LogQueryService>();
        services.AddScoped<OrderStep1TraceLogStorageService>();
        services.AddScoped<OrderStep1ProTraceLogStorageService>();
        services.AddScoped<RouteStopLogQueryService>();
        services.AddScoped<RouteStopTraceLogStorageService>();
        services.AddScoped<RouteStopProTraceLogStorageService>();
        services.AddScoped<PickupTaskTraceLogQueryService>();
        services.AddScoped<PickupTaskTraceLogStorageService>();
        services.AddScoped<PickupTaskProTraceLogStorageService>();

        services.AddSingleton<EventService>();
        services.AddSingleton<LogApiTokenProvider>();

        services.AddSingleton<SignozProTokenProvider>();
        services.AddSingleton<SignozProSyncCheckpointStore>();
        services.AddScoped<SignozProQueryService>();
        services.AddScoped<SignozProTraceIngestionService>();
        services.AddHostedService<SignozProSyncJob>();
        services.AddHostedService<PickupTaskOrderSyncJob>();

        return services;
    }
}
