namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
/// Extension methods for configuration and settings binding.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Binds a configuration section to a settings model using options pattern with data annotation validation.
    /// Also registers the settings as a singleton service for direct injection.
    /// </summary>
    /// <typeparam name="TSettings">The settings model type with a SectionName constant.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration manager.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsAndValidate<TSettings>(
        this IServiceCollection services,
        IConfigurationManager configuration)
        where TSettings : class
    {
        // Get the section name from the static property or constant
        string sectionName = GetSectionName<TSettings>();

        services
            .AddOptions<TSettings>()
            .Bind(configuration.GetRequiredSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Also register the settings as singleton for direct injection
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);

        return services;
    }

    /// <summary>
    /// Gets the section name from the TSettings type (looks for SectionName constant).
    /// </summary>
    private static string GetSectionName<TSettings>() where TSettings : class
    {
        var sectionNameProperty = typeof(TSettings).GetProperty(
            "SectionName",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);

        if (sectionNameProperty != null)
        {
            return (string?)sectionNameProperty.GetValue(null) ?? typeof(TSettings).Name;
        }

        var sectionNameField = typeof(TSettings).GetField(
            "SectionName",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);

        if (sectionNameField != null)
        {
            return (string?)sectionNameField.GetValue(null) ?? typeof(TSettings).Name;
        }

        return typeof(TSettings).Name;
    }

    public static IServiceCollection AddTMSDbQuery(this IServiceCollection services, string database)
    {
        services.AddKeyedScoped(database, (sp, _) =>
        {
            return new ApplicationDbQuery(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<ApplicationDbQuery>>(), database);
        });
        return services;
    }

    public static bool IsDevelopmentOrLocal(this IWebHostEnvironment env)
    {
        return env.IsDevelopment() || env.IsLocal();
    }

    public static bool IsLocal(this IWebHostEnvironment env)
    {
        return env.IsEnvironment("Local");
    }
}

