using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.DeveloperTool.Blazor.Entities;

public sealed class RouteCheckPoint
{
    public Guid Id { get; set; }
    public double Lon { get; set; }
    public double Lat { get; set; }
    public string Address { get; set; } = default!;
    public int Km { get; set; }
    public int Order { get; set; }

    [ForeignKey(nameof(Template))]
    public Guid TemplateId { get; set; }
    public RouteCheckPointTemplate Template { get; set; } = default!;
}
