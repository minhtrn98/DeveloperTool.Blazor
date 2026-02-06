namespace TMS.DeveloperTool.Blazor.Features.Routing.Models;

public sealed class PostOffice
{
    public Guid Id { get; set; }
    public string PostOfficeCode { get; set; } = string.Empty;
    public string PostOfficeName { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public double Longitude { get; set; }
    public double Latitude { get; set; }
}
