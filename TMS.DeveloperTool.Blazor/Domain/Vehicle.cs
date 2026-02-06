using System.ComponentModel.DataAnnotations;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class Vehicle
{
    [Key]
    public string LicensePlate { get; set; } = default!;
    public double LastOdo { get; set; } = default!;
    public bool IsMoving { get; set; }
}
