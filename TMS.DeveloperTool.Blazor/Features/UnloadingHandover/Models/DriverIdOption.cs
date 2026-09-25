using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Features.UnloadingHandover.Models;

public sealed class DriverIdOption(string driverId) : IDisplaySearchItem
{
    public string DriverId { get; } = driverId;

    public string DisplayString => DriverId;

    public bool Like(string searchTerm) => DriverId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
