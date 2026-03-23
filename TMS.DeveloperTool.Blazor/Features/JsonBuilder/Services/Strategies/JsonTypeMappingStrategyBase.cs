using TMS.DeveloperTool.Blazor.Features.JsonBuilder.Models;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services.Strategies;

public abstract class JsonTypeMappingStrategyBase : IJsonTypeMappingStrategy
{
    public abstract string JsonType { get; }

    public abstract Task<IReadOnlyDictionary<string, JsonKeyMapping>> BuildMappingsAsync();

    public virtual IReadOnlyDictionary<string, JsonKeyValueBuilder> KeyValueBuilders { get; } =
        new Dictionary<string, JsonKeyValueBuilder>(StringComparer.OrdinalIgnoreCase);

    public virtual async Task<string?> LoadTemplateAsync(IWebHostEnvironment webHostEnvironment)
    {
        string[] candidatePaths =
        [
            Path.Combine(webHostEnvironment.ContentRootPath, "Templates", GetTemplateFileName()),
            Path.Combine(webHostEnvironment.WebRootPath ?? string.Empty, "Templates", GetTemplateFileName())
        ];

        string? filePath = candidatePaths.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        string templateContent = await File.ReadAllTextAsync(filePath);
        return FormatTemplate(templateContent);
    }

    protected virtual string GetTemplateFileName()
    {
        return $"{JsonType}.json";
    }

    protected virtual string FormatTemplate(string templateContent)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset localStartOfDay = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

        return templateContent
            .Replace("{{datetime}}", localStartOfDay.ToString("O"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{yyyyMMdd}}", localStartOfDay.ToString("yyyyMMdd"), StringComparison.OrdinalIgnoreCase);
    }
}