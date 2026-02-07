namespace TMS.DeveloperTool.Blazor.Features.Pairing.Models;

public sealed class PairingRequest
{
    public Guid VehicleId { get; set; }
    public Guid PairingReasonTypeId { get; set; }
    public double Odometer { get; set; }
    public string Address { get; set; } = string.Empty;
    public int ActionType { get; set; }
    public int CheckStatus { get; set; }
    public int PlanningType { get; set; }
    public Guid PlanningId { get; set; }
    public bool AutoCreateMaintenancePlan { get; set; }
    public string PostOfficeCode { get; set; } = string.Empty;
    public List<PairingAssignmentImage> AssignmentImages { get; set; } = [];
}

public sealed class PairingAssignmentImage
{
    public Guid ImageId { get; set; }
    public int Type { get; set; }
}
