using TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Services;

public sealed class DeliveryManifestService(IManifestApi manifestApi)
{
    public Task<PagedResult<ManifestListItemDto>> SearchAsync(
        SearchManifestRequest request, CancellationToken cancellationToken)
        => manifestApi.SearchAsync(request, cancellationToken);

    public Task<List<GetOrderInManifestResponse>> GetOrdersInManifestAsync(
        Guid manifestId, CancellationToken cancellationToken)
        => manifestApi.GetOrdersInManifestAsync(manifestId, null, cancellationToken);

    public Task<GetManifestOrderDetailResponse?> GetManifestOrderDetailAsync(
        Guid manifestId, string orderId, CancellationToken cancellationToken)
        => manifestApi.GetManifestOrderDetailAsync(manifestId, orderId, cancellationToken);
}
