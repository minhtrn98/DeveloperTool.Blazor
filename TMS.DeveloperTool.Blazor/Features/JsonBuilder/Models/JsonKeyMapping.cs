namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed class JsonKeyMapping(List<object> options, List<DependentValueMapping>? dependentMappings = null)
{
    public List<object> Options { get; } = options;
    public List<DependentValueMapping> DependentMappings { get; } = dependentMappings ?? [];
}

public sealed record DependentValueContext(object? OldValue, object? NewValue, object? OldParentValue, object? NewParentValue);

public sealed class DependentValueMapping(
    string relatedKeyName,
    Dictionary<object, object> valueMappings,
    Func<DependentValueContext, object?>? valueResolver = null)
{
    private readonly Func<DependentValueContext, object?> _valueResolver = valueResolver ?? DefaultValueResolver;
    private readonly bool _hasCustomValueResolver = valueResolver is not null;

    public string RelatedKeyName { get; } = relatedKeyName;
    public Dictionary<object, object> ValueMappings { get; } = valueMappings;

    public bool TryResolveValue(object? oldValue, object? oldParentValue, object? newParentValue, out object? resolvedValue)
    {
        bool hasMappedValue = JsonValueMappingMatcher.TryGetMappedValue(ValueMappings, newParentValue, out object? mappedValue);
        if (!hasMappedValue && !_hasCustomValueResolver)
        {
            resolvedValue = null;
            return false;
        }

        DependentValueContext context = new(oldValue, mappedValue, oldParentValue, newParentValue);
        resolvedValue = _valueResolver(context);

        if (!_hasCustomValueResolver)
        {
            return hasMappedValue;
        }

        return true;
    }

    private static object? DefaultValueResolver(DependentValueContext context)
    {
        return context.NewValue;
    }
}
