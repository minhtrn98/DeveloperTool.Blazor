using TMS.DeveloperTool.Blazor.Features.Dashboard.Services;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskOrderSync.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskOrderTimeline.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;
using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;
using TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;
using TMS.DeveloperTool.Blazor.Features.UnloadingHandover.Services;
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
        services.AddScoped<PickupTaskOrderProStorageService>();
        services.AddScoped<UnloadingHandoverProStorageService>();
        services.AddScoped<DashboardProStorageService>();

        services.AddSingleton<EventService>();
        services.AddSingleton<LogApiTokenProvider>();

        services.AddSingleton<SignozProTokenProvider>();
        services.AddSingleton<SignozProSyncCheckpointStore>();
        services.AddScoped<SignozProQueryService>();
        services.AddScoped<SignozProTraceIngestionService>();

        return services;
    }

    /// <summary>
    /// Adds the background sync jobs unless <see cref="BackgroundJobsOptions.Enabled"/> is false.
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        BackgroundJobsOptions options = configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ?? new();
        if (!options.Enabled)
        {
            return services;
        }

        services.AddHostedService<SignozProSyncJob>();
        services.AddHostedService<PickupTaskOrderSyncJob>();

        return services;
    }
}
