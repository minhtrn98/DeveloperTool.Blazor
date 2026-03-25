using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Pairing.Contracts;

public sealed record SwapDriverRequest
{
    public required Guid VehicleId { get; init; }
    public required double Odometer { get; init; }
    public required ActionType ActionType { get; init; }
    public required Guid DriverRequest { get; init; }
}
