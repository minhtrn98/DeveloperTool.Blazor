namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed class JsonKey(string path, string keyName, object? currentValue, bool isSupported, List<object>? options = null)
{
    public string Path { get; set; } = path;
    public string KeyName { get; set; } = keyName;
    public object? CurrentValue { get; set; } = currentValue;
    public bool IsSupported { get; set; } = isSupported;
    public List<object> Options { get; set; } = options ?? [];
}