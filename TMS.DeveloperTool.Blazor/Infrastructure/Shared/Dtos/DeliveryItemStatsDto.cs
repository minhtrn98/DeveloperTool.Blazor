namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record DeliveryItemStatsDto
{
    public required Guid DeliveryManifestId { get; init; }
    public required long Total { get; init; }
    public required long Delivering { get; init; }
    public required long OnVehicle { get; init; }
    public required long Delivered { get; init; }
    public required long DeliveryRescheduled { get; init; }
    public required long PendingProcessing { get; init; }
    public required long Returning { get; init; }
    public required long Forwarded { get; init; }
    public required long Lost { get; init; }
    public required long Confiscated { get; init; }
    public required long Destroyed { get; init; }
}
