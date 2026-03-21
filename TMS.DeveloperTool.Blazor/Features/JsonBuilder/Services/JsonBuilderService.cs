using System.Text.Encodings.Web;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public sealed class JsonBuilderService
{
    private readonly Dictionary<string, IJsonTypeMappingStrategy> _strategiesByType;
    private readonly Dictionary<string, Task<Dictionary<string, JsonKeyMapping>>> _mappingsCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonBuilderService(IEnumerable<IJsonTypeMappingStrategy> strategies)
    {
        _strategiesByType = strategies.ToDictionary(s => s.JsonType, s => s, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetJsonTypes()
    {
        return _strategiesByType.Keys.OrderBy(x => x).ToList();
    }

    public async Task<List<JsonKey>> ParseJsonAndExtractKeys(string jsonString, string jsonType)
    {
        List<JsonKey> keys = [];
        Dictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);

        try
        {
            JsonNode? jsonNode = JsonNode.Parse(jsonString);
            if (jsonNode != null)
            {
                ExtractKeysRecursive(jsonNode, "", keys, mappings);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON, return empty list
        }
        return keys;
    }

    private static void ExtractKeysRecursive(JsonNode node, string currentPath, List<JsonKey> keys, Dictionary<string, JsonKeyMapping> mappings)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                string path = string.IsNullOrEmpty(currentPath) ? property.Key : $"{currentPath}.{property.Key}";
                if (property.Value is JsonObject or JsonArray)
                {
                    ExtractKeysRecursive(property.Value, path, keys, mappings);
                }
                else
                {
                    bool isSupported = mappings.ContainsKey(property.Key);
                    var jsonKey = new JsonKey(path, property.Key, ExtractNodeValue(property.Value), isSupported);
                    keys.Add(jsonKey);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                string path = $"{currentPath}[{i}]";
                ExtractKeysRecursive(array[i]!, path, keys, mappings);
            }
        }
    }

    public async Task LoadDropdownOptionsAsync(List<JsonKey> keys, string jsonType)
    {
        Dictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);

        foreach (JsonKey key in keys.Where(k => k.IsSupported))
        {
            if (mappings.TryGetValue(key.KeyName, out JsonKeyMapping? mapping))
            {
                key.Options = [.. mapping.Options];

                // For self-mapped keys, convert the current raw value (e.g. numeric) back to display option.
                DependentValueMapping? selfMapping = mapping.DependentMappings
                    .FirstOrDefault(dm => string.Equals(dm.RelatedKeyName, key.KeyName, StringComparison.OrdinalIgnoreCase));
                if (selfMapping != null && TryGetMappedKeyByValue(selfMapping.ValueMappings, key.CurrentValue, out object? displayValue))
                {
                    key.CurrentValue = displayValue;
                }
            }
        }
    }

    public async Task<string> ApplyValueChange(string originalJson, string jsonType, List<JsonKey> keys, JsonKey changedKey, object newValue)
    {
        Dictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);
        changedKey.CurrentValue = newValue;

        object valueToWrite = newValue;
        if (mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? changedKeyMapping))
        {
            DependentValueMapping? selfMapping = changedKeyMapping.DependentMappings
                .FirstOrDefault(dm => string.Equals(dm.RelatedKeyName, changedKey.KeyName, StringComparison.OrdinalIgnoreCase));
            if (selfMapping != null && TryGetMappedValue(selfMapping.ValueMappings, newValue, out object? mappedSelfValue))
            {
                valueToWrite = mappedSelfValue;
            }
        }

        string updatedJson = UpdateJsonValue(originalJson, changedKey.Path, valueToWrite);
        if (!mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? mapping) || mapping.DependentMappings.Count == 0)
        {
            return updatedJson;
        }

        foreach (var dependent in mapping.DependentMappings)
        {
            if (!TryGetMappedValue(dependent.ValueMappings, newValue, out object? dependentValue))
            {
                continue;
            }

            string relatedPath = BuildRelatedPath(changedKey.Path, dependent.RelatedKeyName);
            foreach (var relatedKey in keys.Where(k => string.Equals(k.Path, relatedPath, StringComparison.OrdinalIgnoreCase)))
            {
                bool isSelfMapping = string.Equals(relatedKey.Path, changedKey.Path, StringComparison.OrdinalIgnoreCase);
                relatedKey.CurrentValue = isSelfMapping ? newValue : dependentValue;

                if (!isSelfMapping)
                {
                    updatedJson = UpdateJsonValue(updatedJson, relatedKey.Path, dependentValue);
                }
            }
        }

        return updatedJson;
    }

    private async Task<Dictionary<string, JsonKeyMapping>> GetMappingsByType(string jsonType)
    {
        if (!_strategiesByType.TryGetValue(jsonType, out IJsonTypeMappingStrategy? strategy))
        {
            return [];
        }

        Task<Dictionary<string, JsonKeyMapping>> buildTask;
        lock (_cacheLock)
        {
            if (!_mappingsCache.TryGetValue(jsonType, out buildTask!))
            {
                buildTask = strategy.BuildMappings();
                _mappingsCache[jsonType] = buildTask;
            }
        }

        return await buildTask;
    }

    public static string UpdateJsonValue(string originalJson, string path, object newValue)
    {
        try
        {
            var jsonNode = JsonNode.Parse(originalJson);
            if (jsonNode != null)
            {
                SetValueByPath(jsonNode, path, newValue);
                return jsonNode.ToJsonString(_jsonOptions);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON
        }

        return originalJson;
    }

    private static void SetValueByPath(JsonNode node, string path, object newValue)
    {
        string[] parts = [.. path.Split(['.', '['], StringSplitOptions.RemoveEmptyEntries).Select(p => p.TrimEnd(']'))];

        JsonNode current = node;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            string part = parts[i];
            if (int.TryParse(part, out int index) && current is JsonArray array)
            {
                current = array[index]!;
            }
            else if (current is JsonObject obj)
            {
                current = obj[part]!;
            }
        }

        string lastPart = parts[^1];
        if (int.TryParse(lastPart, out int lastIndex) && current is JsonArray arr)
        {
            JsonNode? existingNode = arr[lastIndex];
            arr[lastIndex] = CreateTypedNode(existingNode, newValue);
        }
        else if (current is JsonObject obj)
        {
            JsonNode? existingNode = obj[lastPart];
            obj[lastPart] = CreateTypedNode(existingNode, newValue);
        }
    }

    private static string BuildRelatedPath(string sourcePath, string relatedKeyName)
    {
        string parentPath = GetParentPath(sourcePath);
        if (string.IsNullOrEmpty(parentPath))
        {
            return relatedKeyName;
        }

        return $"{parentPath}.{relatedKeyName}";
    }

    private static string GetParentPath(string path)
    {
        int lastSeparatorIndex = path.LastIndexOf('.');
        if (lastSeparatorIndex < 0)
        {
            return string.Empty;
        }

        return path[..lastSeparatorIndex];
    }

    private static bool TryGetMappedValue(Dictionary<object, object> mappings, object sourceValue, out object? mappedValue)
    {
        if (mappings.TryGetValue(sourceValue, out object directMappedValue))
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

    private static bool TryGetMappedKeyByValue(Dictionary<object, object> mappings, object? sourceValue, out object? mappedKey)
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

    private static object? ExtractNodeValue(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out string? stringValue))
            {
                return stringValue;
            }

            if (jsonValue.TryGetValue<bool>(out bool boolValue))
            {
                return boolValue;
            }

            if (jsonValue.TryGetValue<int>(out int intValue))
            {
                return intValue;
            }

            if (jsonValue.TryGetValue<long>(out long longValue))
            {
                return longValue;
            }

            if (jsonValue.TryGetValue<decimal>(out decimal decimalValue))
            {
                return decimalValue;
            }

            if (jsonValue.TryGetValue<double>(out double doubleValue))
            {
                return doubleValue;
            }

            if (jsonValue.TryGetValue<JsonElement>(out JsonElement element))
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number when element.TryGetInt64(out long longNumber) => longNumber,
                    JsonValueKind.Number when element.TryGetDecimal(out decimal decimalNumber) => decimalNumber,
                    JsonValueKind.String => element.GetString(),
                    _ => element.ToString()
                };
            }
        }

        return node.ToJsonString();
    }

    private static JsonNode? CreateTypedNode(JsonNode? existingNode, object newValue)
    {
        string rawValue = newValue?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(rawValue))
        {
            return null;
        }

        if (existingNode is JsonValue existingValue)
        {
            if (existingValue.TryGetValue<string>(out _))
            {
                return JsonValue.Create(rawValue);
            }

            if (existingValue.TryGetValue<int>(out _) && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return JsonValue.Create(intValue);
            }

            if (existingValue.TryGetValue<long>(out _) && long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
            {
                return JsonValue.Create(longValue);
            }

            if (existingValue.TryGetValue<decimal>(out _) && decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
            {
                return JsonValue.Create(decimalValue);
            }

            if (existingValue.TryGetValue<double>(out _) && double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
            {
                return JsonValue.Create(doubleValue);
            }

            if (existingValue.TryGetValue<bool>(out _) && bool.TryParse(rawValue, out bool boolValue))
            {
                return JsonValue.Create(boolValue);
            }
        }

        if (bool.TryParse(rawValue, out bool parsedBool))
        {
            return JsonValue.Create(parsedBool);
        }

        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedLong))
        {
            return JsonValue.Create(parsedLong);
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedDecimal))
        {
            return JsonValue.Create(parsedDecimal);
        }

        return JsonValue.Create(rawValue);
    }

}