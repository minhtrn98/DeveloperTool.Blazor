namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

internal static class JsonValueMappingMatcher
{
    public static bool TryGetMappedValue(Dictionary<object, object> mappings, object? sourceValue, out object? mappedValue)
    {
        if (sourceValue is not null && mappings.TryGetValue(sourceValue, out object? directMappedValue))
        {
            mappedValue = directMappedValue;
            return true;
        }

        string sourceText = sourceValue?.ToString() ?? string.Empty;
        foreach ((object key, object value) in mappings)
        {
            if (string.Equals(key?.ToString(), sourceText, StringComparison.OrdinalIgnoreCase))
            {
                mappedValue = value;
                return true;
            }
        }

        mappedValue = null;
        return false;
    }

    public static bool TryGetMappedKeyByValue(Dictionary<object, object> mappings, object? sourceValue, out object? mappedKey)
    {
        foreach ((object key, object value) in mappings)
        {
            if (ValuesEquivalent(value, sourceValue))
            {
                mappedKey = key;
                return true;
            }
        }

        mappedKey = null;
        return false;
    }

    private static bool ValuesEquivalent(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.Equals(right))
        {
            return true;
        }

        string leftText = left.ToString() ?? string.Empty;
        string rightText = right.ToString() ?? string.Empty;
        return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);
    }
}