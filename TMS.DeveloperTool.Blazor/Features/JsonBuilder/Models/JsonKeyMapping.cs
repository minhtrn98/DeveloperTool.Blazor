namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed class JsonKeyMapping(List<object> options, List<DependentValueMapping>? dependentMappings = null)
{
    public List<object> Options { get; } = options;
    public List<DependentValueMapping> DependentMappings { get; } = dependentMappings ?? [];
}

public sealed class DependentValueMapping(string relatedKeyName, Dictionary<object, object> valueMappings)
{
    public string RelatedKeyName { get; } = relatedKeyName;
    public Dictionary<object, object> ValueMappings { get; } = valueMappings;
}
