namespace TMS.DeveloperTool.Blazor.Infrastructure.Caching;

public sealed class RedisCacheItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public TimeSpan? Ttl { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
