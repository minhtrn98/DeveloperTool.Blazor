namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskOrderDraft(
    string DraftId)
{
    public List<PickupTaskOrderItemDraft> Items { get; set; } = [];
}

public sealed record PickupTaskOrderItemDraft(
    string PickupTaskId,
    string DraftId,
    string DraftItemId);