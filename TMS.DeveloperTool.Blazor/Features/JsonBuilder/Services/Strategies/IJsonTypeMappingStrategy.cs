using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;

public interface IJsonTypeMappingStrategy
{
    string JsonType { get; }

    Task<IReadOnlyDictionary<string, JsonKeyMapping>> BuildMappings();

    Task<string?> LoadTemplateAsync(IWebHostEnvironment webHostEnvironment);
}
