using System.Text.Json;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

public sealed record RouteStopMessageFields(
    string DriverName,
    string VehicleLicensePlate,
    string ItemCount,
    string Weight,
    string ActionObjectId,
    string IsFullPending,
    string MarkStopCompleted)
{
    public static readonly RouteStopMessageFields Empty = new(
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}

public static class RouteStopMessageFieldsParser
{
    public static RouteStopMessageFields Parse(string messageDetail)
    {
        if (string.IsNullOrWhiteSpace(messageDetail))
        {
            return RouteStopMessageFields.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(messageDetail);
            JsonElement root = document.RootElement;

            return new RouteStopMessageFields(
                ReadValue(root, "DriverName"),
                ReadValue(root, "VehicleLicensePlate"),
                ReadValue(root, "ItemCount"),
                ReadValue(root, "Weight"),
                ReadValue(root, "ActionObjectId"),
                ReadValue(root, "IsFullPending"),
                ReadValue(root, "MarkStopCompleted"));
        }
        catch (JsonException)
        {
            return RouteStopMessageFields.Empty;
        }
    }

    /// <summary>
    /// Reads the vehicle position ("Latitude"/"Longitude") from a RouteStopRecorded message detail;
    /// null for either value when it's missing, not a number, or the JSON is invalid.
    /// </summary>
    public static (decimal? Latitude, decimal? Longitude) ParseLocation(string messageDetail)
    {
        if (string.IsNullOrWhiteSpace(messageDetail))
        {
            return (null, null);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(messageDetail);
            JsonElement root = document.RootElement;
            return (ReadDecimal(root, "Latitude"), ReadDecimal(root, "Longitude"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDecimal(out decimal number)
                ? number
                : null;

    private static string ReadValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => value.GetRawText()
        };
    }
}
