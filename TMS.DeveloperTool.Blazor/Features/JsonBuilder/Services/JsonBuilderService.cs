using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Hosting;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public sealed class JsonBuilderService
{
    private readonly IReadOnlyDictionary<string, IJsonTypeMappingStrategy> _strategiesByType;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>>> _mappingsCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonBuilderService(IEnumerable<IJsonTypeMappingStrategy> strategies, IWebHostEnvironment webHostEnvironment)
    {
        _strategiesByType = strategies.ToDictionary(s => s.JsonType, s => s, StringComparer.OrdinalIgnoreCase);
        _webHostEnvironment = webHostEnvironment;
    }

    public IReadOnlyList<string> GetJsonTypes()
    {
        return _strategiesByType.Keys.OrderBy(x => x).ToList();
    }

    public async Task<string?> LoadTemplateAsync(string jsonType)
    {
        if (string.IsNullOrWhiteSpace(jsonType))
        {
            return null;
        }

        string fileName = $"{jsonType}.json";
        string[] candidatePaths =
        [
            Path.Combine(_webHostEnvironment.ContentRootPath, "Templates", fileName),
            Path.Combine(_webHostEnvironment.WebRootPath ?? string.Empty, "Templates", fileName)
        ];

        string? filePath = candidatePaths.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(filePath);
    }

    public async Task<List<JsonKey>> ParseJsonAndExtractKeys(string jsonString, string jsonType)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return [];
        }

        List<JsonKey> keys = [];
        IReadOnlyDictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);

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

    private static void ExtractKeysRecursive(JsonNode node, string currentPath, List<JsonKey> keys, IReadOnlyDictionary<string, JsonKeyMapping> mappings)
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
                    var jsonKey = new JsonKey(path, property.Key, JsonNodeValueFactory.ExtractNodeValue(property.Value), isSupported);
                    keys.Add(jsonKey);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is null)
                {
                    continue;
                }

                string path = $"{currentPath}[{i}]";
                ExtractKeysRecursive(array[i]!, path, keys, mappings);
            }
        }
        else if (node is JsonValue)
        {
            string keyName = GetKeyNameFromPath(currentPath);
            bool isSupported = mappings.ContainsKey(keyName);
            keys.Add(new JsonKey(currentPath, keyName, JsonNodeValueFactory.ExtractNodeValue(node), isSupported));
        }
    }

    private static string GetKeyNameFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        int lastDot = path.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < path.Length - 1)
        {
            return path[(lastDot + 1)..];
        }

        int lastOpenBracket = path.LastIndexOf('[');
        int lastCloseBracket = path.LastIndexOf(']');
        if (lastOpenBracket >= 0 && lastCloseBracket > lastOpenBracket)
        {
            return path[(lastOpenBracket + 1)..lastCloseBracket];
        }

        return path;
    }

    public async Task LoadDropdownOptionsAsync(List<JsonKey> keys, string jsonType)
    {
        IReadOnlyDictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);

        foreach (JsonKey key in keys.Where(k => k.IsSupported))
        {
            if (mappings.TryGetValue(key.KeyName, out JsonKeyMapping? mapping))
            {
                key.Options = [.. mapping.Options];

                // For self-mapped keys, convert the current raw value (e.g. numeric) back to display option.
                DependentValueMapping? selfMapping = mapping.DependentMappings
                    .FirstOrDefault(dm => string.Equals(dm.RelatedKeyName, key.KeyName, StringComparison.OrdinalIgnoreCase));
                if (selfMapping != null && JsonMappingValueResolver.TryGetMappedKeyByValue(selfMapping.ValueMappings, key.CurrentValue, out object? displayValue))
                {
                    key.CurrentValue = displayValue;
                }
            }
        }
    }

    public async Task<string> ApplyValueChange(string originalJson, string jsonType, List<JsonKey> keys, JsonKey changedKey, object newValue)
    {
        IReadOnlyDictionary<string, JsonKeyMapping> mappings = await GetMappingsByType(jsonType);
        changedKey.CurrentValue = newValue;

        object valueToWrite = newValue;
        if (mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? changedKeyMapping))
        {
            DependentValueMapping? selfMapping = changedKeyMapping.DependentMappings
                .FirstOrDefault(dm => string.Equals(dm.RelatedKeyName, changedKey.KeyName, StringComparison.OrdinalIgnoreCase));
            if (selfMapping != null && JsonMappingValueResolver.TryGetMappedValue(selfMapping.ValueMappings, newValue, out object? mappedSelfValue))
            {
                valueToWrite = mappedSelfValue!;
            }
        }

        string updatedJson = UpdateJsonValue(originalJson, changedKey.Path, valueToWrite);
        if (!mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? mapping) || mapping.DependentMappings.Count == 0)
        {
            return updatedJson;
        }

        foreach (var dependent in mapping.DependentMappings)
        {
            if (!JsonMappingValueResolver.TryGetMappedValue(dependent.ValueMappings, newValue, out object? dependentValue))
            {
                continue;
            }

            string relatedPath = JsonPathHelper.BuildRelatedPath(changedKey.Path, dependent.RelatedKeyName);
            foreach (var relatedKey in keys.Where(k => string.Equals(k.Path, relatedPath, StringComparison.OrdinalIgnoreCase)))
            {
                bool isSelfMapping = string.Equals(relatedKey.Path, changedKey.Path, StringComparison.OrdinalIgnoreCase);
                relatedKey.CurrentValue = isSelfMapping ? newValue : dependentValue;

                if (!isSelfMapping)
                {
                    updatedJson = UpdateJsonValue(updatedJson, relatedKey.Path, dependentValue!);
                }
            }
        }

        return updatedJson;
    }

    private async Task<IReadOnlyDictionary<string, JsonKeyMapping>> GetMappingsByType(string jsonType)
    {
        if (!_strategiesByType.TryGetValue(jsonType, out IJsonTypeMappingStrategy? strategy))
        {
            return new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase);
        }

        Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>> lazyMappings = _mappingsCache.GetOrAdd(
            jsonType,
            _ => new Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>>(
                async () => await strategy.BuildMappings(),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return await lazyMappings.Value;
    }

    public static string UpdateJsonValue(string originalJson, string path, object newValue)
    {
        try
        {
            var jsonNode = JsonNode.Parse(originalJson);
            if (jsonNode != null)
            {
                if (!JsonPathHelper.TrySetValueByPath(jsonNode, path, newValue))
                {
                    return originalJson;
                }

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