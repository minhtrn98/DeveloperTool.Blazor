using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public interface IJsonTypeMappingStrategy
{
    string JsonType { get; }

    Task<Dictionary<string, JsonKeyMapping>> BuildMappings();

    Task<string?> LoadTemplateAsync(IWebHostEnvironment webHostEnvironment)
    {
        return Task.FromResult<string?>(null);
    }
}
