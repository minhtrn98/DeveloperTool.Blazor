using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

internal static class JsonNodeValueFactory
{
    public static object? ExtractNodeValue(JsonNode? node)
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

    public static JsonNode? CreateTypedNode(JsonNode? existingNode, object newValue)
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