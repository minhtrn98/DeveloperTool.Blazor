using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;

public delegate Task<object> JsonKeyValueBuilder();

public interface IJsonTypeMappingStrategy
{
    string JsonType { get; }

    Task<IReadOnlyDictionary<string, JsonKeyMapping>> BuildMappingsAsync();

    IReadOnlyDictionary<string, JsonKeyValueBuilder> KeyValueBuilders { get; }

    Task<string?> LoadTemplateAsync(IWebHostEnvironment webHostEnvironment);

    Task SendRequestAsync(string jsonInput, CancellationToken cancellationToken = default);
}
