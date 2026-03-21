using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Tests.Features.JsonBuilder.Models;

public class JsonValueMappingMatcherTests
{
    [Fact]
    public void TryGetMappedValue_WithDirectKeyMatch_ReturnsMappedValue()
    {
        Dictionary<object, object> mappings = new() { [1] = "One" };

        bool ok = JsonValueMappingMatcher.TryGetMappedValue(mappings, 1, out object? value);

        ok.Should().BeTrue();
        value.Should().Be("One");
    }

    [Fact]
    public void TryGetMappedValue_WithCaseInsensitiveTextMatch_ReturnsMappedValue()
    {
        Dictionary<object, object> mappings = new() { ["manual"] = 1 };

        bool ok = JsonValueMappingMatcher.TryGetMappedValue(mappings, "MANUAL", out object? value);

        ok.Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void TryGetMappedValue_WhenMissing_ReturnsFalse()
    {
        Dictionary<object, object> mappings = new() { ["a"] = "A" };

        bool ok = JsonValueMappingMatcher.TryGetMappedValue(mappings, "b", out object? value);

        ok.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetMappedKeyByValue_WithEquivalentStringValue_ReturnsKey()
    {
        Dictionary<object, object> mappings = new() { ["Auto"] = 2 };

        bool ok = JsonValueMappingMatcher.TryGetMappedKeyByValue(mappings, "2", out object? key);

        ok.Should().BeTrue();
        key.Should().Be("Auto");
    }
}
