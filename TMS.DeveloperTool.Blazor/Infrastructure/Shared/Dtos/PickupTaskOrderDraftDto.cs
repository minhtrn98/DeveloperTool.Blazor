namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PickupTaskOrderDraftDto(
    string DraftId)
{
    public List<PickupTaskOrderDraftItemDto> Items { get; set; } = [];
}

public sealed record PickupTaskOrderDraftItemDto(
    string PickupTaskId,
    string DraftId,
    string DraftItemId);