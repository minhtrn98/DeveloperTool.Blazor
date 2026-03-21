using System.Text.Json.Nodes;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.JsonBuilder.Services;

public class JsonPathHelperTests
{
    #region TrySetValueByPath Tests

    [Fact]
    public void TrySetValueByPath_WithSimpleRootProperty_ShouldSetValue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""name"": ""John""}") as JsonObject;
        var path = "name";
        var newValue = "Jane";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node!["name"]!.GetValue<string>().Should().Be("Jane");
    }

    [Fact]
    public void TrySetValueByPath_WithNestedProperty_ShouldSetValue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""user"": {""name"": ""John""}}") as JsonObject;
        var path = "user.name";
        var newValue = "Jane";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node!["user"]!["name"]!.GetValue<string>().Should().Be("Jane");
    }

    [Fact]
    public void TrySetValueByPath_WithDeeplyNestedProperty_ShouldSetValue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""user"": {""profile"": {""name"": ""John""}}}") as JsonObject;
        var path = "user.profile.name";
        var newValue = "Jane";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node!["user"]!["profile"]!["name"]!.GetValue<string>().Should().Be("Jane");
    }

    [Theory]
    [InlineData(0, "updated")]
    [InlineData(1, "modified")]
    [InlineData(2, "changed")]
    public void TrySetValueByPath_WithArrayIndex_ShouldSetValue(int index, string newValue)
    {
        // Arrange
        var json = @"[""first"", ""second"", ""third""]";
        var node = JsonNode.Parse(json) as JsonArray;
        var path = index.ToString();

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node![index]!.GetValue<string>().Should().Be(newValue);
    }

    [Fact]
    public void TrySetValueByPath_WithNestedArrayAccess_ShouldSetValue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""items"": [{""name"": ""item1""}, {""name"": ""item2""}]}") as JsonObject;
        var path = "items.0.name";
        var newValue = "updated_item";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node!["items"]![0]!["name"]!.GetValue<string>().Should().Be("updated_item");
    }

    [Fact]
    public void TrySetValueByPath_WithBracketNotation_ShouldSetValue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""items"": [{""name"": ""item1""}]}") as JsonObject;
        var path = "items[0].name";
        var newValue = "updated_item";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node!["items"]![0]!["name"]!.GetValue<string>().Should().Be("updated_item");
    }

    [Fact]
    public void TrySetValueByPath_WithMixedDotAndBracketNotation_ShouldSetValue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""data"": {""values"": [{""id"": 1}]}}") as JsonObject;
        var path = "data.values[0].id";
        var newValue = 999;

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, newValue);

        // Assert
        result.Should().BeTrue();
        node!["data"]!["values"]![0]!["id"]!.GetValue<int>().Should().Be(999);
    }

    [Fact]
    public void TrySetValueByPath_WithEmptyPath_ShouldReturnFalse()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""name"": ""John""}") as JsonObject;
        var path = "";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, "NewValue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetValueByPath_WithInvalidPropertyPath_ShouldReturnFalse()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""name"": ""John""}") as JsonObject;
        var path = "nonexistent.property";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, "NewValue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetValueByPath_WithOutOfBoundsArrayIndex_ShouldReturnFalse()
    {
        // Arrange
        var json = @"[""first"", ""second""]";
        var node = JsonNode.Parse(json) as JsonArray;
        var path = "5";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, "NewValue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetValueByPath_WithNegativeArrayIndex_ShouldReturnFalse()
    {
        // Arrange
        var json = @"[""first"", ""second""]";
        var node = JsonNode.Parse(json) as JsonArray;
        var path = "-1";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, "NewValue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetValueByPath_WithArrayAccessOnObject_ShouldReturnFalse()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""name"": ""John""}") as JsonObject;
        var path = "0";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, "NewValue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetValueByPath_WithPropertyAccessOnArray_ShouldReturnFalse()
    {
        // Arrange
        var json = @"[""first"", ""second""]";
        var node = JsonNode.Parse(json) as JsonArray;
        var path = "name";

        // Act
        var result = JsonPathHelper.TrySetValueByPath(node!, path, "NewValue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetValueByPath_WithVariousDataTypes_ShouldPreserveType()
    {
        // Arrange
        var json = @"{
            ""stringVal"": ""text"",
            ""intVal"": 42,
            ""boolVal"": true,
            ""doubleVal"": 3.14
        }";
        var node = JsonNode.Parse(json) as JsonObject;

        // Act & Assert - Update each type
        JsonPathHelper.TrySetValueByPath(node!, "stringVal", "updated").Should().BeTrue();
        node!["stringVal"]!.GetValue<string>().Should().Be("updated");

        // Note: Type conversion is handled by JsonNodeValueFactory
        JsonPathHelper.TrySetValueByPath(node, "intVal", "99").Should().BeTrue();
        JsonPathHelper.TrySetValueByPath(node, "boolVal", "false").Should().BeTrue();
        JsonPathHelper.TrySetValueByPath(node, "doubleVal", "2.71").Should().BeTrue();
    }

    #endregion

    #region BuildRelatedPath Tests

    [Fact]
    public void BuildRelatedPath_FromRootLevel_ShouldReturnJustTheKeyName()
    {
        // Arrange
        var sourcePath = "name";
        var relatedKeyName = "age";

        // Act
        var result = JsonPathHelper.BuildRelatedPath(sourcePath, relatedKeyName);

        // Assert
        result.Should().Be("age");
    }

    [Fact]
    public void BuildRelatedPath_FromNestedPath_ShouldBuildCorrectPath()
    {
        // Arrange
        var sourcePath = "user.profile";
        var relatedKeyName = "email";

        // Act
        var result = JsonPathHelper.BuildRelatedPath(sourcePath, relatedKeyName);

        // Assert
        result.Should().Be("user.email");
    }

    [Fact]
    public void BuildRelatedPath_FromDeeplyNestedPath_ShouldBuildCorrectPath()
    {
        // Arrange
        var sourcePath = "user.profile.personal";
        var relatedKeyName = "phone";

        // Act
        var result = JsonPathHelper.BuildRelatedPath(sourcePath, relatedKeyName);

        // Assert
        result.Should().Be("user.profile.phone");
    }

    [Fact]
    public void BuildRelatedPath_WithArrayIndexInPath_ShouldBuildCorrectPath()
    {
        // Arrange
        var sourcePath = "items.0.details";
        var relatedKeyName = "metadata";

        // Act
        var result = JsonPathHelper.BuildRelatedPath(sourcePath, relatedKeyName);

        // Assert
        result.Should().Be("items.0.metadata");
    }

    [Fact]
    public void BuildRelatedPath_WithEmptySourcePath_ShouldReturnJustTheKeyName()
    {
        // Arrange
        var sourcePath = "";
        var relatedKeyName = "name";

        // Act
        var result = JsonPathHelper.BuildRelatedPath(sourcePath, relatedKeyName);

        // Assert
        result.Should().Be("name");
    }

    [Fact]
    public void BuildRelatedPath_WithBracketNotationInPath_ShouldBuildCorrectPath()
    {
        // Arrange
        var sourcePath = "items[0].name";
        var relatedKeyName = "description";

        // Act
        var result = JsonPathHelper.BuildRelatedPath(sourcePath, relatedKeyName);

        // Assert
        result.Should().Be("items[0].description");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void ComplexScenario_MultipleUpdatesInComplexStructure_ShouldUpdateAllValues()
    {
        // Arrange
        var json = @"{
            ""users"": [
                {
                    ""name"": ""John"",
                    ""details"": {
                        ""age"": 30,
                        ""email"": ""john@example.com""
                    }
                },
                {
                    ""name"": ""Jane"",
                    ""details"": {
                        ""age"": 28,
                        ""email"": ""jane@example.com""
                    }
                }
            ]
        }";
        var node = JsonNode.Parse(json) as JsonObject;

        // Act
        var result1 = JsonPathHelper.TrySetValueByPath(node!, "users.0.name", "John Doe");
        var result2 = JsonPathHelper.TrySetValueByPath(node!, "users[0].details.age", "31");
        var result3 = JsonPathHelper.TrySetValueByPath(node!, "users[1].details.email", "jane.doe@example.com");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();

        node!["users"]![0]!["name"]!.GetValue<string>().Should().Be("John Doe");
        node!["users"]![0]!["details"]!["email"]!.GetValue<string>().Should().Be("john@example.com");
        node!["users"]![1]!["details"]!["email"]!.GetValue<string>().Should().Be("jane.doe@example.com");
    }

    [Fact]
    public void ComplexScenario_BuildPathFromArrayItem_ShouldCreateCorrectPath()
    {
        // Arrange
        var paths = new[] { "users.0.details", "items[2].config", "data.values.0.metadata" };
        var relatedKey = "updated";

        // Act
        var results = paths.Select(p => JsonPathHelper.BuildRelatedPath(p, relatedKey)).ToList();

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().Be("users.0.updated");
        results[1].Should().Be("items[2].updated");
        results[2].Should().Be("data.values.0.updated");
    }

    #endregion
}
