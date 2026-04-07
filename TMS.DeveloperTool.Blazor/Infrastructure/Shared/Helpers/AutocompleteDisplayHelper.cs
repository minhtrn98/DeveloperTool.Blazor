using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

public static class AutocompleteDisplayHelper
{
    public static string GetDisplayString<TItem>(TItem? item) where TItem : class, IDisplaySearchItem
    {
        return item?.GetDisplayString() ?? string.Empty;
    }
}
