using Serilog;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog for the application.
    /// </summary>
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

        return builder;
    }
}
