using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public sealed class JsonBuilderService(
    IEnumerable<IJsonTypeMappingStrategy> strategies,
    IWebHostEnvironment webHostEnvironment,
    TimeProvider timeProvider)
{
    private sealed record CachedBuilderResult(Lazy<Task<object>> ValueFactory, DateTimeOffset ExpiresAt);

    private static readonly TimeSpan KeyValueBuilderCacheTtl = TimeSpan.FromMinutes(1);

    private readonly IReadOnlyDictionary<string, IJsonTypeMappingStrategy> _strategiesByJsonType = strategies.ToDictionary(s => s.JsonType, s => s, StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>>> _jsonKeyMappingsCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CachedBuilderResult> _keyValueBuilderCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonBuilderService(IEnumerable<IJsonTypeMappingStrategy> strategies, IWebHostEnvironment webHostEnvironment)
        : this(strategies, webHostEnvironment, TimeProvider.System)
    {
    }

    public IReadOnlyList<string> GetJsonTypes()
    {
        return _strategiesByJsonType.Keys.OrderBy(x => x).ToList();
    }

    public async Task<string?> LoadTemplateAsync(string jsonType)
    {
        if (string.IsNullOrWhiteSpace(jsonType))
        {
            return null;
        }

        if (!_strategiesByJsonType.TryGetValue(jsonType, out IJsonTypeMappingStrategy? strategy))
        {
            return null;
        }

        return await strategy.LoadTemplateAsync(webHostEnvironment);
    }

    public async Task SendRequestAsync(string jsonInput, string jsonType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonInput))
        {
            throw new ArgumentException("JSON input is required.", nameof(jsonInput));
        }

        if (string.IsNullOrWhiteSpace(jsonType))
        {
            throw new ArgumentException("JSON type is required.", nameof(jsonType));
        }

        if (!_strategiesByJsonType.TryGetValue(jsonType, out IJsonTypeMappingStrategy? strategy))
        {
            throw new InvalidOperationException($"Unsupported json type: {jsonType}.");
        }

        await strategy.SendRequestAsync(jsonInput, cancellationToken);
    }

    public async Task<List<JsonKey>> ParseJsonAndExtractKeys(string jsonString, string jsonType)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return [];
        }

        List<JsonKey> keys = [];
        IReadOnlyDictionary<string, JsonKeyMapping> mappings = await GetMappingsAsync(jsonType);

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

    public async Task<string> ApplyKeyValueBuildersAsync(string originalJson, string jsonType)
    {
        if (string.IsNullOrWhiteSpace(originalJson))
        {
            return originalJson;
        }

        if (!_strategiesByJsonType.TryGetValue(jsonType, out IJsonTypeMappingStrategy? strategy) || strategy.KeyValueBuilders.Count == 0)
        {
            return originalJson;
        }

        try
        {
            JsonNode? jsonNode = JsonNode.Parse(originalJson);
            if (jsonNode is null)
            {
                return originalJson;
            }

            foreach ((string keyName, JsonKeyValueBuilder builder) in strategy.KeyValueBuilders)
            {
                object builtValue = await GetKeyValueBuilderResultAsync(jsonType, keyName, builder);
                ReplaceMatchingPropertyValues(jsonNode, keyName, builtValue);
            }

            return jsonNode.ToJsonString(_jsonOptions);
        }
        catch (JsonException)
        {
            return originalJson;
        }
    }

    private static void ExtractKeysRecursive(JsonNode node, string currentPath, List<JsonKey> keys, IReadOnlyDictionary<string, JsonKeyMapping> mappings)
    {
        if (node is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                string path = string.IsNullOrEmpty(currentPath) ? property.Key : $"{currentPath}.{property.Key}";
                if (property.Value is JsonObject or JsonArray)
                {
                    ExtractKeysRecursive(property.Value, path, keys, mappings);
                }
                else
                {
                    bool isSupported = mappings.ContainsKey(property.Key);
                    JsonKey jsonKey = new JsonKey(path, property.Key, JsonNodeValueFactory.ExtractNodeValue(property.Value), isSupported);
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

    private static void ReplaceMatchingPropertyValues(JsonNode node, string keyName, object builtValue)
    {
        if (node is JsonObject jsonObject)
        {
            List<string> propertyNames = [.. jsonObject.Select(p => p.Key)];
            foreach (string propertyName in propertyNames)
            {
                JsonNode? propertyNode = jsonObject[propertyName];

                if (string.Equals(propertyName, keyName, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject[propertyName] = JsonNodeValueFactory.CreateTypedNode(propertyNode, builtValue);
                    propertyNode = jsonObject[propertyName];
                }

                if (propertyNode is JsonObject or JsonArray)
                {
                    ReplaceMatchingPropertyValues(propertyNode, keyName, builtValue);
                }
            }

            return;
        }

        if (node is not JsonArray jsonArray)
        {
            return;
        }

        foreach (JsonNode? item in jsonArray)
        {
            if (item is JsonObject or JsonArray)
            {
                ReplaceMatchingPropertyValues(item, keyName, builtValue);
            }
        }
    }

    private async Task<object> GetKeyValueBuilderResultAsync(string jsonType, string keyName, JsonKeyValueBuilder builder)
    {
        string cacheKey = $"{jsonType}:{keyName}";
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (_keyValueBuilderCache.TryGetValue(cacheKey, out CachedBuilderResult? cachedResult) && cachedResult.ExpiresAt > now)
        {
            return await cachedResult.ValueFactory.Value;
        }

        CachedBuilderResult newCachedResult = new(
            new Lazy<Task<object>>(() => builder(), LazyThreadSafetyMode.ExecutionAndPublication),
            now.Add(KeyValueBuilderCacheTtl));

        CachedBuilderResult activeCachedResult = _keyValueBuilderCache.AddOrUpdate(
            cacheKey,
            newCachedResult,
            (_, existingCachedResult) => existingCachedResult.ExpiresAt > now ? existingCachedResult : newCachedResult);

        try
        {
            return await activeCachedResult.ValueFactory.Value;
        }
        catch
        {
            _keyValueBuilderCache.TryRemove(cacheKey, out _);
            throw;
        }
    }

    public async Task LoadDropdownOptionsAsync(List<JsonKey> keys, string jsonType)
    {
        IReadOnlyDictionary<string, JsonKeyMapping> mappings = await GetMappingsAsync(jsonType);

        foreach (JsonKey key in keys.Where(k => k.IsSupported))
        {
            if (mappings.TryGetValue(key.KeyName, out JsonKeyMapping? mapping))
            {
                key.Options = [.. mapping.Options];

                // For self-mapped keys, convert the current raw value (e.g. numeric) back to display option.
                DependentValueMapping? selfMapping = mapping.DependentMappings
                    .FirstOrDefault(dm => string.Equals(dm.RelatedKeyName, key.KeyName, StringComparison.OrdinalIgnoreCase));
                if (selfMapping != null && JsonValueMappingMatcher.TryGetMappedKeyByValue(selfMapping.ValueMappings, key.CurrentValue, out object? displayValue))
                {
                    key.CurrentValue = displayValue;
                }
            }
        }
    }

    public async Task<string> ApplyValueChange(string originalJson, string jsonType, List<JsonKey> keys, JsonKey changedKey, object newValue)
    {
        IReadOnlyDictionary<string, JsonKeyMapping> mappings = await GetMappingsAsync(jsonType);
        object? oldParentValue = changedKey.CurrentValue;
        changedKey.CurrentValue = newValue;

        object valueToWrite = newValue;
        if (mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? changedKeyMapping))
        {
            DependentValueMapping? selfMapping = changedKeyMapping.DependentMappings
                .FirstOrDefault(dm => string.Equals(dm.RelatedKeyName, changedKey.KeyName, StringComparison.OrdinalIgnoreCase));
            if (selfMapping != null && selfMapping.TryResolveValue(oldParentValue, oldParentValue, newValue, out object? mappedSelfValue))
            {
                valueToWrite = mappedSelfValue!;
            }
        }

        string updatedJson = UpdateJsonValue(originalJson, changedKey.Path, valueToWrite);
        if (!mappings.TryGetValue(changedKey.KeyName, out JsonKeyMapping? mapping) || mapping.DependentMappings.Count == 0)
        {
            return updatedJson;
        }

        foreach (DependentValueMapping dependent in mapping.DependentMappings)
        {
            string relatedPath = JsonPathHelper.BuildRelatedPath(changedKey.Path, dependent.RelatedKeyName);
            foreach (JsonKey? relatedKey in keys.Where(k => string.Equals(k.Path, relatedPath, StringComparison.OrdinalIgnoreCase)))
            {
                bool isSelfMapping = string.Equals(relatedKey.Path, changedKey.Path, StringComparison.OrdinalIgnoreCase);
                if (isSelfMapping)
                {
                    relatedKey.CurrentValue = newValue;
                    continue;
                }

                object? oldValue = relatedKey.CurrentValue;
                if (!dependent.TryResolveValue(oldValue, oldParentValue, newValue, out object? dependentValue))
                {
                    continue;
                }

                relatedKey.CurrentValue = dependentValue;

                updatedJson = UpdateJsonValue(updatedJson, relatedKey.Path, dependentValue!);
            }
        }

        return updatedJson;
    }

    private async Task<IReadOnlyDictionary<string, JsonKeyMapping>> GetMappingsAsync(string jsonType)
    {
        if (!_strategiesByJsonType.TryGetValue(jsonType, out IJsonTypeMappingStrategy? strategy))
        {
            return new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase);
        }

        Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>> lazyMappings = _jsonKeyMappingsCache.GetOrAdd(
            jsonType,
            _ => new Lazy<Task<IReadOnlyDictionary<string, JsonKeyMapping>>>(
                async () => await strategy.BuildMappingsAsync(),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return await lazyMappings.Value;
    }

    public static string UpdateJsonValue(string originalJson, string path, object newValue)
    {
        try
        {
            JsonNode? jsonNode = JsonNode.Parse(originalJson);
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