using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

/// <summary>
/// Cung cap helper chuyen doi gia tri giua JsonNode va CLR value phu hop.
/// </summary>
internal static class JsonNodeValueFactory
{
    /// <summary>
    /// Trich xuat gia tri tu JsonNode ve kieu CLR phu hop (string, bool, so, hoac JSON text).
    /// </summary>
    /// <param name="node">Node can trich xuat gia tri.</param>
    /// <returns>
    /// Gia tri da trich xuat; null neu node null.
    /// Neu node khong phai JsonValue thi tra ve JSON string cua node.
    /// </returns>
    /// <example>
    /// <code>
    /// var value = JsonNodeValueFactory.ExtractNodeValue(JsonValue.Create(123));
    /// // value = 123
    /// </code>
    /// </example>
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

    /// <summary>
    /// Tao JsonNode moi tu input value, uu tien giu kieu cua existingNode neu co.
    /// </summary>
    /// <param name="existingNode">Node hien tai de suy ra kieu mong muon.</param>
    /// <param name="newValue">Gia tri moi can gan.</param>
    /// <returns>
    /// JsonNode da ep kieu theo existingNode, hoac parse theo thu tu bool -> long -> decimal -> string.
    /// Tra ve null neu newValue rong.
    /// </returns>
    /// <example>
    /// <code>
    /// var current = JsonValue.Create(10);
    /// var updated = JsonNodeValueFactory.CreateTypedNode(current, "25");
    /// // updated.GetValue&lt;int&gt;() = 25
    /// </code>
    /// </example>
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