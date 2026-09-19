using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Features.RouteStop.Models;

public sealed class ActionObjectIdOption(string actionObjectId) : IDisplaySearchItem
{
    public string ActionObjectId { get; } = actionObjectId;

    public string DisplayString => ActionObjectId;

    public bool Like(string searchTerm) => ActionObjectId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
