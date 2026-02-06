using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class RouteCheckPoint
{
    public RouteCheckPoint()
    {
    }

    public static RouteCheckPoint Copy(RouteCheckPoint data)
    {
        return new RouteCheckPoint
        {
            Id = data.Id,
            Lon = data.Lon,
            Lat = data.Lat,
            Address = data.Address,
            Km = data.Km,
            Order = data.Order,
            TemplateId = data.TemplateId
        };
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public double Lon { get; set; }
    public double Lat { get; set; }
    public string Address { get; set; } = default!;
    public int Km { get; set; }
    public int Order { get; set; }

    [ForeignKey(nameof(Template))]
    public Guid TemplateId { get; set; }
    public RouteCheckPointTemplate Template { get; set; } = default!;
}
