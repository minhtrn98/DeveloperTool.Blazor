using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public sealed class JsonBuilderService
{
    // Add other repositories as needed

    private readonly Dictionary<string, Func<Task<List<string>>>> _keyMappings = new()
    {
        {
            "pickupPostOfficeCode", async () =>
            {
                // Simulate fetching driver IDs from a repository
                await Task.Delay(100); // Simulate async work
                return
                [
                    "HBH",
                    "K3B",
                    "K3C"
                ];
            }
        }
    };

    public List<JsonKey> ParseJsonAndExtractKeys(string jsonString)
    {
        var keys = new List<JsonKey>();
        try
        {
            var jsonNode = JsonNode.Parse(jsonString);
            if (jsonNode != null)
            {
                ExtractKeysRecursive(jsonNode, "", keys);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON, return empty list
        }
        return keys;
    }

    private void ExtractKeysRecursive(JsonNode node, string currentPath, List<JsonKey> keys)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                string path = string.IsNullOrEmpty(currentPath) ? property.Key : $"{currentPath}.{property.Key}";
                if (property.Value is JsonObject or JsonArray)
                {
                    ExtractKeysRecursive(property.Value, path, keys);
                }
                else
                {
                    bool isSupported = _keyMappings.ContainsKey(property.Key);
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
                ExtractKeysRecursive(array[i], path, keys);
            }
        }
    }

    public async Task LoadDropdownOptionsAsync(List<JsonKey> keys)
    {
        foreach (var key in keys.Where(k => k.IsSupported))
        {
            if (_keyMappings.TryGetValue(key.KeyName, out var func))
            {
                key.Options = await func();
            }
        }
    }

    public string UpdateJsonValue(string originalJson, string path, object newValue)
    {
        // For simple top-level keys, use string replacement to preserve formatting
        if (!path.Contains('.') && !path.Contains('['))
        {
            string key = path;
            string valueStr = newValue?.ToString() ?? "";
            // Escape quotes in value if any
            valueStr = valueStr.Replace("\"", "\\\"");
            string pattern = $"\"{Regex.Escape(key)}\": \"[^\"]*\"";
            string replacement = $"\"{key}\": \"{valueStr}\"";
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
                    return jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver(), Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
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
                current = array[index];
            }
            else if (current is JsonObject obj)
            {
                current = obj[part];
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
}