namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Helpers;

public static class VietnamTimeHelper
{
    public const string DisplayFormat = "dd/MM/yyyy HH:mm:ss";

    // IANA id works on Windows too since .NET 6 (ICU), so this is independent of the server's local time zone.
    public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static DateTimeOffset Now => ToVietnamTime(DateTimeOffset.UtcNow);

    public static DateTimeOffset ToVietnamTime(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, TimeZone);

    public static string Format(DateTimeOffset value) => ToVietnamTime(value).ToString(DisplayFormat);

    public static string Format(DateTimeOffset? value) => value.HasValue ? Format(value.Value) : string.Empty;

    // Interprets a wall-clock date/time picked in the UI as Vietnam time.
    public static DateTimeOffset FromVietnamLocal(DateTime localDateTime)
    {
        DateTime unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TimeZone.GetUtcOffset(unspecified));
    }
}
