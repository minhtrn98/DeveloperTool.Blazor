using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Features.Routing.Models;

public sealed class PostOffice : IDisplaySearchItem
{
    public Guid Id { get; set; }
    public string PostOfficeCode { get; set; } = string.Empty;
    public string PostOfficeName { get; set; } = string.Empty;
    public string WardId { get; set; } = string.Empty;
    public string? WardName { get; set; }
    public string ProvinceId { get; set; } = string.Empty;
    public string? ProvinceName { get; set; }
    public string StreetAddress { get; set; } = string.Empty;
    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public string GetDisplayString()
    {
        return $"{PostOfficeCode} - {PostOfficeName}";
    }

    public bool Like(string searchTerm)
    {
        return PostOfficeCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               PostOfficeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }
}
