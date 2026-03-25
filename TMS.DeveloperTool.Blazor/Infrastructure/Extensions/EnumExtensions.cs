using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Extensions;

public static class EnumExtensions
{
    public static string ToDescriptionString(this Enum val)
    {
        DescriptionAttribute[] attributes = (DescriptionAttribute[])val
           .GetType()
           .GetField(val.ToString())!
           .GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : val.ToString();
    }

    public static IEnumerable<(int Value, string Code, string Description)> ToList<T>() where T : Enum
    {
        foreach (var enumValue in Enum.GetValues(typeof(T)))
        {
            var intValue = (int)enumValue;
            var code = enumValue.ToString()!;
            var description = ((Enum)enumValue).ToDescriptionString();
            yield return (intValue, code, description);
        }
    }

    public static string ToEnumMemberValueString(this Enum val)
    {
        FieldInfo? field = val.GetType().GetField(val.ToString());
        if (field is null)
        {
            return val.ToString();
        }

        EnumMemberAttribute? enumMember = (EnumMemberAttribute?)Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute));
        return string.IsNullOrWhiteSpace(enumMember?.Value) ? val.ToString() : enumMember.Value;
    }

    public static Dictionary<string, string> ToValueDescriptionMap<T>() where T : Enum
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Enum enumValue in Enum.GetValues(typeof(T)).Cast<Enum>())
        {
            string value = enumValue.ToEnumMemberValueString();
            string description = enumValue.ToDescriptionString();
            result[value] = description;
        }

        return result;
    }
}
