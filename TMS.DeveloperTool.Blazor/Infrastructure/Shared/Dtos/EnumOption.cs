using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

/// <summary>Wraps an enum-like value for <c>MudAutocomplete</c> (see CLAUDE.md "Dropdowns").</summary>
public sealed record EnumOption(string Value, string Description) : IDisplaySearchItem
{
    public string DisplayString => Description;

    public bool Like(string searchTerm) => Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
