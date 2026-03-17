using TMS.DeveloperTool.Blazor.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed class JsonKey
{
    public string Path { get; set; }
    public string KeyName { get; set; }
    public object? CurrentValue { get; set; }
    public bool IsSupported { get; set; }
    public List<string> Options { get; set; } = [];

    public JsonKey(string path, string keyName, object? currentValue, bool isSupported, List<string>? options = null)
    {
        Path = path;
        KeyName = keyName;
        CurrentValue = currentValue;
        IsSupported = isSupported;
        Options = options ?? [];
    }
}