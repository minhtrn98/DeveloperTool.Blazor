using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Extensions;

public static class EnumExtensions
{
    public static string ToDescriptionString(this Enum val)
    {
        var attributes = (DescriptionAttribute[])val
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
}