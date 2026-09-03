using MudBlazor.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

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
        services.AddSettingsAndValidate<MyRedisOptions>(config);
        services.AddSettingsAndValidate<RabbitMqConfig>(config);
        services.AddSettingsAndValidate<JwtOptions>(config);
        services.AddSettingsAndValidate<ApiUrlsOptions>(config);
        services.AddSettingsAndValidate<MenuVisibilityOptions>(config);
        services.AddSettingsAndValidate<QuanLyXeOptions>(config);
        services.AddSettingsAndValidate<LogApiOptions>(config);
        services.AddSettingsAndValidate<FeUrlsOptions>(config);
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
