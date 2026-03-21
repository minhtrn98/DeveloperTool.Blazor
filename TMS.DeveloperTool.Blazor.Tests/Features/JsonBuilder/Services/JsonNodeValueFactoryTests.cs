using System.Text.Json.Nodes;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.JsonBuilder.Services;

public class JsonNodeValueFactoryTests
{
    [Fact]
    public void ExtractNodeValue_WithNullNode_ReturnsNull()
    {
        JsonNode? node = null;

        object? result = JsonNodeValueFactory.ExtractNodeValue(node);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractNodeValue_WithStringValue_ReturnsString()
    {
        JsonNode node = JsonValue.Create("abc")!;

        object? result = JsonNodeValueFactory.ExtractNodeValue(node);

        result.Should().Be("abc");
    }

    [Fact]
    public void ExtractNodeValue_WithBooleanValue_ReturnsBoolean()
    {
        JsonNode node = JsonValue.Create(true)!;

        object? result = JsonNodeValueFactory.ExtractNodeValue(node);

        result.Should().Be(true);
    }

    [Fact]
    public void ExtractNodeValue_WithIntegerValue_ReturnsInteger()
    {
        JsonNode node = JsonValue.Create(42)!;

        object? result = JsonNodeValueFactory.ExtractNodeValue(node);

        result.Should().Be(42);
    }

    [Fact]
    public void ExtractNodeValue_WithLongValue_ReturnsLong()
    {
        long expected = 9_000_000_000;
        JsonNode node = JsonValue.Create(expected)!;

        object? result = JsonNodeValueFactory.ExtractNodeValue(node);

        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractNodeValue_WithObjectNode_ReturnsJsonString()
    {
        JsonNode node = JsonNode.Parse("{\"a\":1}")!;

        object? result = JsonNodeValueFactory.ExtractNodeValue(node);

        result.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void CreateTypedNode_WithNullNewValue_ReturnsNull()
    {
        JsonNode? existing = JsonValue.Create("x");

        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(existing, null!);

        result.Should().BeNull();
    }

    [Fact]
    public void CreateTypedNode_WithExistingString_KeepsStringType()
    {
        JsonNode existing = JsonValue.Create("old")!;

        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(existing, 123);

        result.Should().NotBeNull();
        result!.GetValue<string>().Should().Be("123");
    }

    [Fact]
    public void CreateTypedNode_WithExistingInt_ParsesToInt()
    {
        JsonNode existing = JsonValue.Create(10)!;

        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(existing, "25");

        result.Should().NotBeNull();
        result!.GetValue<int>().Should().Be(25);
    }

    [Fact]
    public void CreateTypedNode_WithExistingBool_ParsesToBool()
    {
        JsonNode existing = JsonValue.Create(true)!;

        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(existing, "false");

        result.Should().NotBeNull();
        result!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public void CreateTypedNode_WithExistingDecimal_ParsesToDecimal()
    {
        JsonNode existing = JsonValue.Create(1.5m)!;

        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(existing, "2.75");

        result.Should().NotBeNull();
        result!.GetValue<decimal>().Should().Be(2.75m);
    }

    [Fact]
    public void CreateTypedNode_WithoutExistingNode_ParsesBooleanFirst()
    {
        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(null, "true");

        result.Should().NotBeNull();
        result!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void CreateTypedNode_WithoutExistingNode_ParsesLongBeforeDecimal()
    {
        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(null, "123");

        result.Should().NotBeNull();
        result!.GetValue<long>().Should().Be(123L);
    }

    [Fact]
    public void CreateTypedNode_WithoutExistingNode_ParsesDecimal()
    {
        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(null, "123.45");

        result.Should().NotBeNull();
        result!.GetValue<decimal>().Should().Be(123.45m);
    }

    [Fact]
    public void CreateTypedNode_WithoutExistingNode_FallsBackToString()
    {
        JsonNode? result = JsonNodeValueFactory.CreateTypedNode(null, "abc");

        result.Should().NotBeNull();
        result!.GetValue<string>().Should().Be("abc");
    }
}
