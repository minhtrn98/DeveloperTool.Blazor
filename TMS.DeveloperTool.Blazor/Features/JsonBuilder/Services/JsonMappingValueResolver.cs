using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

internal static class JsonMappingValueResolver
{
    public static bool TryGetMappedKeyByValue(Dictionary<object, object> mappings, object? sourceValue, out object? mappedKey)
    {
        return JsonValueMappingMatcher.TryGetMappedKeyByValue(mappings, sourceValue, out mappedKey);
    }
}