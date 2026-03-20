using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
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
                    var jsonKey = new JsonKey(path, property.Key, property.Value?.ToString(), isSupported);
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
            }
        }
    }

    public async Task<string> ApplyValueChange(string originalJson, string jsonType, List<JsonKey> keys, JsonKey changedKey, string newValue)
    {
        Dictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);
        changedKey.CurrentValue = newValue;

        string updatedJson = UpdateJsonValue(originalJson, changedKey.Path, newValue);
        if (!mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? mapping) || mapping.DependentMappings.Count == 0)
        {
            return updatedJson;
        }

        foreach (var dependent in mapping.DependentMappings)
        {
            if (!dependent.ValueMappings.TryGetValue(newValue, out var dependentValue))
            {
                continue;
            }

            string relatedPath = BuildRelatedPath(changedKey.Path, dependent.RelatedKeyName);
            foreach (var relatedKey in keys.Where(k => string.Equals(k.Path, relatedPath, StringComparison.OrdinalIgnoreCase)))
            {
                relatedKey.CurrentValue = dependentValue;
                updatedJson = UpdateJsonValue(updatedJson, relatedKey.Path, dependentValue);
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
        // For simple top-level keys, use string replacement to preserve formatting
        if (!path.Contains('.') && !path.Contains('['))
        {
            string key = path;
            string valueStr = newValue?.ToString() ?? "";
            // Escape quotes in value if any
            valueStr = valueStr.Replace("\"", "\\\"");
            string pattern = $"\"{Regex.Escape(key)}\": (null|\"[^\"]*\")";
            string replacement = string.IsNullOrEmpty(valueStr) ? $"\"{key}\": null" : $"\"{key}\": \"{valueStr}\"";
            return Regex.Replace(originalJson, pattern, replacement);
        }
        else
        {
            // Fallback to JsonNode for nested paths
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
            arr[lastIndex] = JsonValue.Create(newValue);
        }
        else if (current is JsonObject obj)
        {
            obj[lastPart] = JsonValue.Create(newValue);
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

}