using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Features.PickupTaskTrace.Models;

public sealed class DeliveryLineIdOption(string deliveryLineId) : IDisplaySearchItem
{
    public string DeliveryLineId { get; } = deliveryLineId;

    public string DisplayString => DeliveryLineId;

    public bool Like(string searchTerm) => DeliveryLineId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
