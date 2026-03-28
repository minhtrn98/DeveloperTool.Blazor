using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

public sealed class ConfirmPickupRequest
{
    public required string PickupTaskId { get; init; }
    public required PickupMode Mode { get; init; } = PickupMode.Full;
    public required bool IsShipmentBatch { get; init; }
    public required int TotalOrders { get; init; }
    public required List<PickedOrderDto> PickedOrders { get; init; } = [];
    public required List<PickedWorkerDto> Workers { get; init; } = [];
}

public sealed record PickedOrderDto
{
    public required string OrderId { get; init; }
    public decimal? Weight { get; init; }
    public decimal? L { get; init; }
    public decimal? W { get; init; }
    public decimal? H { get; init; }
    public GoodsType GoodsType { get; init; } = GoodsType.Document;
    public decimal? DeclaredValue { get; init; }
    public string? Note { get; init; }
    public bool? IsDirty { get; init; } = false;
    public List<PickedItemDto> Items { get; init; } = [];
}

public sealed record PickedItemDto
{
    public required string OrderId { get; init; }
    public required string OrderItemId { get; init; }
    public decimal? Weight { get; init; }
    public decimal? L { get; init; }
    public decimal? W { get; init; }
    public decimal? H { get; init; }
    public string? Note { get; init; }
    public bool? IsDirty { get; init; } = false;
}

public sealed record PickedWorkerDto
{
    public required Guid EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public required string EmployeeName { get; init; }
    public required decimal WorkContributionRatio { get; init; }
}