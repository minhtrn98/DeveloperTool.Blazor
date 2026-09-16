using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;

public sealed class PickupTaskIdOption(string pickupTaskId) : IDisplaySearchItem
{
    public string PickupTaskId { get; } = pickupTaskId;

    public string DisplayString => PickupTaskId;

    public bool Like(string searchTerm) => PickupTaskId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
