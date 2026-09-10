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
