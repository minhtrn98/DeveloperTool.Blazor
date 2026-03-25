using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Pairing.Models;

public sealed class PairingRequest
{
    public Guid VehicleId { get; set; }
    public Guid PairingReasonTypeId { get; set; }
    public double Odometer { get; set; }
    public string Address { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public int CheckStatus { get; set; }
    public AssignmentPlanType PlanningType { get; set; }
    public Guid PlanningId { get; set; }
    public bool AutoCreateMaintenancePlan { get; set; }
    public string PostOfficeCode { get; set; } = string.Empty;
    public List<PairingAssignmentImage> AssignmentImages { get; set; } = [];
}

public sealed class ConfirmRequest
{
    public required Guid VehicleId { get; init; }
}

public sealed class PairingAssignmentImage
{
    public Guid ImageId { get; set; }
    public int Type { get; set; }
}
