namespace TMS.DeveloperTool.Blazor.Features.DeliveryArrivalLocation.Models;

/// <summary>Outcome of one fill: arrivals since <see cref="SinceUtc"/> whose vehicle / order position was set.</summary>
public sealed record DeliveryArrivalLocationFillResult(DateTimeOffset SinceUtc, int VehicleFilled, int OrderFilled);
