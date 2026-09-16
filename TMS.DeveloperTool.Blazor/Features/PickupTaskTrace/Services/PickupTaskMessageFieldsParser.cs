using System.Text.Json;
using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Services;

public sealed record PickupTaskMessageFields(
    string DispatchType,
    string DispatchMethod,
    string PmsStatusId,
    string TotalItems,
    string TotalWeight,
    string DeliveryLineId,
    string PickEffortType)
{
    public static readonly PickupTaskMessageFields Empty = new(
        string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}

public static class PickupTaskMessageFieldsParser
{
    public static PickupTaskMessageFields Parse(string messageDetail)
    {
        if (string.IsNullOrWhiteSpace(messageDetail))
        {
            return PickupTaskMessageFields.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(messageDetail);
            JsonElement root = document.RootElement;

            return new PickupTaskMessageFields(
                ReadEnumDescription<DispatchType>(root, "DispatchType"),
                ReadEnumDescription<DispatchMethod>(root, "DispatchMethod"),
                ReadValue(root, "PmsStatusID"),
                ReadValue(root, "TotalItems"),
                ReadValue(root, "TotalWeight"),
                ReadValue(root, "DeliveryLineID"),
                ReadValue(root, "PickEffortType"));
        }
        catch (JsonException)
        {
            return PickupTaskMessageFields.Empty;
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

    private static string ReadEnumDescription<TEnum>(JsonElement element, string propertyName) where TEnum : struct, Enum
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int intValue))
        {
            return ReadValue(element, propertyName);
        }

        return Enum.IsDefined(typeof(TEnum), intValue)
            ? ((TEnum)(object)intValue).ToDescriptionString()
            : intValue.ToString();
    }
}
