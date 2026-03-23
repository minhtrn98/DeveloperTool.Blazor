using Microsoft.AspNetCore.Hosting;
using System.Text.Json.Nodes;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;
using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;

namespace TMS.DeveloperTool.Blazor.Tests.Features.JsonBuilder.Services;

public class JsonBuilderServiceTests
{
    [Fact]
    public void GetJsonTypes_ReturnsSortedTypes()
    {
        var env = new Mock<IWebHostEnvironment>();
        var strategies = new IJsonTypeMappingStrategy[]
        {
            new FakeStrategy("ZType"),
            new FakeStrategy("AType")
        };

        var service = new JsonBuilderService(strategies, env.Object);

        IReadOnlyList<string> result = service.GetJsonTypes();

        result.Should().Equal("AType", "ZType");
    }

    [Fact]
    public async Task LoadTemplateAsync_WithUnknownType_ReturnsNull()
    {
        var env = new Mock<IWebHostEnvironment>();
        var service = new JsonBuilderService([new FakeStrategy("Known")], env.Object);

        string? result = await service.LoadTemplateAsync("Unknown");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseJsonAndExtractKeys_WithInvalidJson_ReturnsEmpty()
    {
        var env = new Mock<IWebHostEnvironment>();
        var service = new JsonBuilderService([new FakeStrategy("Type1")], env.Object);

        List<JsonKey> keys = await service.ParseJsonAndExtractKeys("{ invalid json", "Type1");

        keys.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseJsonAndExtractKeys_MarksSupportedByKeyName()
    {
        var env = new Mock<IWebHostEnvironment>();
        var strategy = new FakeStrategy(
            "Type1",
            mappings: new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = new([], null)
            });
        var service = new JsonBuilderService([strategy], env.Object);

        List<JsonKey> keys = await service.ParseJsonAndExtractKeys("{\"obj\":{\"name\":\"john\",\"age\":10}}", "Type1");

        keys.Should().HaveCount(2);
        keys.Single(k => k.KeyName == "name").IsSupported.Should().BeTrue();
        keys.Single(k => k.KeyName == "age").IsSupported.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDropdownOptionsAsync_SelfMapping_ConvertsCurrentValueBackToDisplayValue()
    {
        var env = new Mock<IWebHostEnvironment>();
        var mappings = new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            ["dispatchType"] = new(
            ["Manual", "Auto"],
            [
                new DependentValueMapping(
                    "dispatchType",
                    new Dictionary<object, object>
                    {
                        ["Manual"] = 1,
                        ["Auto"] = 2
                    })
            ])
        };

        var service = new JsonBuilderService([new FakeStrategy("Type1", mappings)], env.Object);
        List<JsonKey> keys =
        [
            new("event.dispatchType", "dispatchType", 2, true)
        ];

        await service.LoadDropdownOptionsAsync(keys, "Type1");

        keys[0].Options.Should().Equal("Manual", "Auto");
        keys[0].CurrentValue.Should().Be("Auto");
    }

    [Fact]
    public async Task ApplyValueChange_UpdatesChangedAndDependentKeys_AndJson()
    {
        var env = new Mock<IWebHostEnvironment>();
        var mappings = new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = new(
            ["A", "B"],
            [
                new DependentValueMapping(
                    "statusName",
                    new Dictionary<object, object>
                    {
                        ["A"] = "Alpha",
                        ["B"] = "Beta"
                    })
            ])
        };

        var service = new JsonBuilderService([new FakeStrategy("Type1", mappings)], env.Object);

        string original = "{\"event\":{\"status\":\"A\",\"statusName\":\"Alpha\"}}";
        List<JsonKey> keys =
        [
            new("event.status", "status", "A", true),
            new("event.statusName", "statusName", "Alpha", true)
        ];
        JsonKey changedKey = keys[0];

        string updated = await service.ApplyValueChange(original, "Type1", keys, changedKey, "B");

        changedKey.CurrentValue.Should().Be("B");
        keys[1].CurrentValue.Should().Be("Beta");

        JsonNode parsed = JsonNode.Parse(updated)!;
        parsed["event"]!["status"]!.GetValue<string>().Should().Be("B");
        parsed["event"]!["statusName"]!.GetValue<string>().Should().Be("Beta");
    }

    [Fact]
    public async Task ApplyValueChange_UsesCachedMappings_BuildMappingsCalledOnce()
    {
        var env = new Mock<IWebHostEnvironment>();
        var strategy = new FakeStrategy(
            "Type1",
            mappings: new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase)
            {
                ["status"] = new([], null)
            });

        var service = new JsonBuilderService([strategy], env.Object);
        List<JsonKey> keys = [new("event.status", "status", "A", true)];

        await service.ApplyValueChange("{\"event\":{\"status\":\"A\"}}", "Type1", keys, keys[0], "B");
        await service.ApplyValueChange("{\"event\":{\"status\":\"B\"}}", "Type1", keys, keys[0], "C");

        strategy.BuildMappingsAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ApplyKeyValueBuildersAsync_ReplacesMatchingKey_WithBuiltArrayValue()
    {
        var env = new Mock<IWebHostEnvironment>();
        var strategy = new FakeStrategy(
            "Type1",
            keyValueBuilders: new Dictionary<string, JsonKeyValueBuilder>(StringComparer.OrdinalIgnoreCase)
            {
                ["orders"] = () => Task.FromResult<object>(new[]
                {
                    new { OrderId = "ORD-001" }
                })
            });
        var service = new JsonBuilderService([strategy], env.Object);

        string original = "{\"orders\":[],\"event\":\"sample\"}";

        string updated = await service.ApplyKeyValueBuildersAsync(original, "Type1");

        JsonNode parsed = JsonNode.Parse(updated)!;
        parsed["orders"]![0]!["OrderId"]!.GetValue<string>().Should().Be("ORD-001");
    }

    [Fact]
    public async Task ApplyKeyValueBuildersAsync_UsesCachedBuilderValue_WithinTtl()
    {
        var env = new Mock<IWebHostEnvironment>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 23, 10, 0, 0, TimeSpan.Zero));
        int buildCount = 0;
        var strategy = new FakeStrategy(
            "Type1",
            keyValueBuilders: new Dictionary<string, JsonKeyValueBuilder>(StringComparer.OrdinalIgnoreCase)
            {
                ["orders"] = () =>
                {
                    buildCount++;
                    return Task.FromResult<object>(new[] { new { OrderId = $"ORD-{buildCount:000}" } });
                }
            });
        var service = new JsonBuilderService([strategy], env.Object, timeProvider);

        string original = "{\"orders\":[]}";

        string first = await service.ApplyKeyValueBuildersAsync(original, "Type1");
        timeProvider.Advance(TimeSpan.FromSeconds(30));
        string second = await service.ApplyKeyValueBuildersAsync(original, "Type1");

        buildCount.Should().Be(1);
        second.Should().Be(first);
    }

    [Fact]
    public async Task ApplyKeyValueBuildersAsync_RebuildsValue_AfterTtlExpires()
    {
        var env = new Mock<IWebHostEnvironment>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 23, 10, 0, 0, TimeSpan.Zero));
        int buildCount = 0;
        var strategy = new FakeStrategy(
            "Type1",
            keyValueBuilders: new Dictionary<string, JsonKeyValueBuilder>(StringComparer.OrdinalIgnoreCase)
            {
                ["orders"] = () =>
                {
                    buildCount++;
                    return Task.FromResult<object>(new[] { new { OrderId = $"ORD-{buildCount:000}" } });
                }
            });
        var service = new JsonBuilderService([strategy], env.Object, timeProvider);

        string original = "{\"orders\":[]}";

        string first = await service.ApplyKeyValueBuildersAsync(original, "Type1");
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        string second = await service.ApplyKeyValueBuildersAsync(original, "Type1");

        buildCount.Should().Be(2);
        second.Should().NotBe(first);
    }

    [Fact]
    public void UpdateJsonValue_WithInvalidPath_ReturnsOriginalJson()
    {
        string original = "{\"x\":1}";

        string updated = JsonBuilderService.UpdateJsonValue(original, "obj.name", "new");

        updated.Should().Be(original);
    }

    private sealed class FakeStrategy : IJsonTypeMappingStrategy
    {
        private readonly IReadOnlyDictionary<string, JsonKeyMapping> _mappings;
        private readonly IReadOnlyDictionary<string, JsonKeyValueBuilder> _keyValueBuilders;
        private readonly string? _template;

        public FakeStrategy(
            string jsonType,
            IReadOnlyDictionary<string, JsonKeyMapping>? mappings = null,
            IReadOnlyDictionary<string, JsonKeyValueBuilder>? keyValueBuilders = null,
            string? template = null)
        {
            JsonType = jsonType;
            _mappings = mappings ?? new Dictionary<string, JsonKeyMapping>(StringComparer.OrdinalIgnoreCase);
            _keyValueBuilders = keyValueBuilders ?? new Dictionary<string, JsonKeyValueBuilder>(StringComparer.OrdinalIgnoreCase);
            _template = template;
        }

        public int BuildMappingsAsyncCallCount { get; private set; }
        public string JsonType { get; }
        public IReadOnlyDictionary<string, JsonKeyValueBuilder> KeyValueBuilders => _keyValueBuilders;

        public Task<IReadOnlyDictionary<string, JsonKeyMapping>> BuildMappingsAsync()
        {
            BuildMappingsAsyncCallCount++;
            return Task.FromResult(_mappings);
        }

        public Task<string?> LoadTemplateAsync(IWebHostEnvironment webHostEnvironment)
        {
            return Task.FromResult(_template);
        }

        public Task SendRequestAsync(string jsonInput, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
