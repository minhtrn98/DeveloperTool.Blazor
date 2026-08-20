using TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Models;

public sealed class OrderIdOption(string orderId) : IDisplaySearchItem
{
    public string OrderId { get; } = orderId;

    public string DisplayString => OrderId;

    public bool Like(string searchTerm) => OrderId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
