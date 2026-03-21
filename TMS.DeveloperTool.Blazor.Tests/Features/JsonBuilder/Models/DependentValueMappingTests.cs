using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Tests.Features.JsonBuilder.Models;

public class DependentValueMappingTests
{
    [Fact]
    public void TryResolveValue_DefaultResolver_WithMappedParentValue_ReturnsMappedValue()
    {
        var mapping = new DependentValueMapping(
            "statusName",
            new Dictionary<object, object>
            {
                ["A"] = "Alpha",
                ["B"] = "Beta"
            });

        bool ok = mapping.TryResolveValue("old", "A", "B", out object? resolved);

        ok.Should().BeTrue();
        resolved.Should().Be("Beta");
    }

    [Fact]
    public void TryResolveValue_DefaultResolver_WhenNoMap_ReturnsFalse()
    {
        var mapping = new DependentValueMapping(
            "statusName",
            new Dictionary<object, object> { ["A"] = "Alpha" });

        bool ok = mapping.TryResolveValue("old", "A", "C", out object? resolved);

        ok.Should().BeFalse();
        resolved.Should().BeNull();
    }

    [Fact]
    public void TryResolveValue_CustomResolver_WhenNoMap_StillUsesResolver()
    {
        var mapping = new DependentValueMapping(
            "pickupTaskId",
            new Dictionary<object, object>(),
            context => $"{context.NewParentValue}-suffix");

        bool ok = mapping.TryResolveValue("AA-1", "AA", "BB", out object? resolved);

        ok.Should().BeTrue();
        resolved.Should().Be("BB-suffix");
    }
}
