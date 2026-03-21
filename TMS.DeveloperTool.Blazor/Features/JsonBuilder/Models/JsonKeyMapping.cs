namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

public sealed class JsonKeyMapping(List<object> options, List<DependentValueMapping>? dependentMappings = null)
{
    public List<object> Options { get; } = options;
    public List<DependentValueMapping> DependentMappings { get; } = dependentMappings ?? [];
}

public sealed record DependentValueContext(object? OldValue, object? NewValue, object? OldParentValue, object? NewParentValue);

public sealed class DependentValueMapping
{
    private readonly Func<DependentValueContext, object?> _valueResolver;

    public string RelatedKeyName { get; }
    public Dictionary<object, object> ValueMappings { get; }

    public DependentValueMapping(
        string relatedKeyName,
        Dictionary<object, object> valueMappings,
        Func<DependentValueContext, object?>? valueResolver = null)
    {
        RelatedKeyName = relatedKeyName;
        ValueMappings = valueMappings;
        _valueResolver = valueResolver ?? DefaultValueResolver;
    }

    public bool TryResolveValue(object? oldValue, object? oldParentValue, object? newParentValue, out object? resolvedValue)
    {
        if (!TryGetMappedValue(newParentValue, out object? mappedValue))
        {
            resolvedValue = null;
            return false;
        }

        DependentValueContext context = new(oldValue, mappedValue, oldParentValue, newParentValue);
        resolvedValue = _valueResolver(context);
        return true;
    }

    private static object? DefaultValueResolver(DependentValueContext context)
    {
        return context.NewValue;
    }

    private bool TryGetMappedValue(object? sourceValue, out object? mappedValue)
    {
        if (sourceValue is not null && ValueMappings.TryGetValue(sourceValue, out object? directMappedValue))
        {
            mappedValue = directMappedValue;
            return true;
        }

        string sourceText = sourceValue?.ToString() ?? string.Empty;
        foreach ((object key, object value) in ValueMappings)
        {
            if (string.Equals(key?.ToString(), sourceText, StringComparison.OrdinalIgnoreCase))
            {
                mappedValue = value;
                return true;
            }
        }

        mappedValue = null;
        return false;
    }
}
