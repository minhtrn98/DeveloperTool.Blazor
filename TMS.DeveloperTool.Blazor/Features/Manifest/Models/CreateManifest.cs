using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Models;

public sealed class CreateManifest
{
    public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.FullOrder;
    public Employee? Driver { get; set; }
    public List<CreateManifestOrder> Orders { get; set; } = [];
}

public sealed class CreateManifestOrder
{
    public required string OrderId { get; init; }
    public required OrderType OrderType { get; init; }
    public List<CreateManifestItem> Items { get; init; } = [];
}

public sealed class CreateManifestItem
{
    public required string OrderItemId { get; init; }
    public bool IsLoaded { get; set; } = true;
}
