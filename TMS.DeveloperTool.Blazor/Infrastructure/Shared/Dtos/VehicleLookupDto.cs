using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record VehicleLookupDto(Guid Id, string ActualPlate) : IDisplaySearchItem
{
    public string DisplayString => ActualPlate;

    public bool Like(string searchTerm) => ActualPlate.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
