using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class Order
{
    public string OrderId { get; init; } = string.Empty;
    public DateTimeOffset? AcceptedTime { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? SenderName { get; init; }
    public string? SenderAddress { get; init; }
    public string? SenderPostOfficeName { get; init; }
    public string? ReceiverName { get; init; }
    public string? ReceiverAddress { get; init; }
    public string? ReceiverPostOfficeName { get; init; }
    public string? CurrentPostOfficeName { get; init; }
    public string? CurrentStatusId { get; init; }
    public string? CurrentStatusName { get; init; }
    public string? ServiceTypeName { get; init; }
    public decimal? Weight { get; init; }
    public decimal? CodAmount { get; init; }
    public OrderType OrderType { get; init; }
}
