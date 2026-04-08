using Refit;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;
using TMS.DeveloperTool.Blazor.Features.Pairing.Services;
using TMS.DeveloperTool.Blazor.Features.PickupTask.Services;
using TMS.DeveloperTool.Blazor.Features.Routing.Services;
using TMS.DeveloperTool.Blazor.Features.Simulation.Services;
using TMS.DeveloperTool.Blazor.Infrastructure.Http;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;
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
        services.AddTransient<LoggingHttpHandler>();

        // Fleet API with custom Refit settings
        RefitSettings refitSettings = new(new SystemTextJsonContentSerializer(
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }));

        services.AddRefitClient<IFleetAssignmentApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                ApiUrlsOptions apiUrls = sp.GetRequiredService<ApiUrlsOptions>();
                c.BaseAddress = new Uri(apiUrls.Fleet);
            })
            .AddHttpMessageHandler<LoggingHttpHandler>();

        // Pickup Task API
        services.AddRefitClient<IPickupTaskApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                ApiUrlsOptions apiUrls = sp.GetRequiredService<ApiUrlsOptions>();
                c.BaseAddress = new Uri(apiUrls.Order);
            })
            .AddHttpMessageHandler<LoggingHttpHandler>();

        // QuanLyXe Vehicle Status API
        services.AddTransient<QuanLyXeApiKeyHandler>();

        services.AddRefitClient<IVehicleStatusApi>(refitSettings)
            .ConfigureHttpClient((sp, c) =>
            {
                QuanLyXeOptions opts = sp.GetRequiredService<QuanLyXeOptions>();
                c.BaseAddress = new Uri(opts.BaseUrl);
            })
            .AddHttpMessageHandler<QuanLyXeApiKeyHandler>()
            .AddHttpMessageHandler<LoggingHttpHandler>();

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
        services.AddScoped<CreatePickupTaskEventService>();
        services.AddScoped<IJsonTypeMappingStrategy, PickupTaskEventJsonMappingStrategy>();
        services.AddScoped<JsonBuilderService>();
        services.AddScoped<MyEmployeeService>();

        services.AddSingleton<EventService>();
        services.AddSingleton<JwtTokenService>();

        return services;
    }
}
