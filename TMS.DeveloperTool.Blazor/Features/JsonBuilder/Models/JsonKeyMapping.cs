namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed class JsonKeyMapping(List<string> optionLoader, List<DependentValueMapping>? dependentMappings = null)
{
    public List<string> OptionLoader { get; } = optionLoader;
    public List<DependentValueMapping> DependentMappings { get; } = dependentMappings ?? [];
}

public sealed class DependentValueMapping(string relatedKeyName, Dictionary<string, string> valueMappings)
{
    public string RelatedKeyName { get; } = relatedKeyName;
    public Dictionary<string, string> ValueMappings { get; } = valueMappings;
}
