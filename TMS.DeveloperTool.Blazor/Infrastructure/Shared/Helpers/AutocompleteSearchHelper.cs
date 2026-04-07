using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

public static class AutocompleteSearchHelper
{
    public static IEnumerable<TItem> SearchByLike<TItem>(
        IEnumerable<TItem> source,
        string searchTerm,
        bool orderByRelevance = true)
        where TItem : IDisplaySearchItem
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return source;
        }

        return orderByRelevance
            ? source.OrderByDescending(item => item.Like(searchTerm))
            : source.Where(item => item.Like(searchTerm));
    }
}
