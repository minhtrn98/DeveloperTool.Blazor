namespace TMS.DeveloperTool.Blazor.Infrastructure.Configuration;

public sealed class MenuVisibilityOptions
{
    public const string SectionName = "MenuVisibility";

    public string[] LocalNonLocalIpVisibleMenus { get; set; } = [];
}