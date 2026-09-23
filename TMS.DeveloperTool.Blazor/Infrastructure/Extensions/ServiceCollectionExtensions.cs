using MudBlazor.Services;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

/// <summary>
/// Primary service collection extensions orchestrator.
/// Delegates to specific extension classes for organization.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration settings and validates them.
    /// </summary>
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfigurationManager config)
    {
        services.AddSettingsAndValidate<ConnectionStringsOptions>(config);
        services.AddSettingsAndValidate<RabbitMqConfig>(config);
        services.AddSettingsAndValidate<LogApiOptions>(config);
        services.AddSettingsAndValidate<SignozProOptions>(config);

        // Default BackgroundServiceExceptionBehavior is StopHost — an unhandled exception in
        // any single BackgroundService (e.g. SignozProSyncJob) would otherwise crash this
        // entire Blazor Server app for every connected user. Each hosted service is expected to
        // catch and log its own transient failures; this is only a second line of defense.
        services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        return services;
    }

    /// <summary>
    /// Adds UI and component services (MudBlazor, Razor components).
    /// </summary>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMudServices();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddControllers();
        return services;
    }
}
