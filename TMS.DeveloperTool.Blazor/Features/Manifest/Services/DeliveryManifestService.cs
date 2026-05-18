using TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Services;

public sealed class DeliveryManifestService(IManifestApi manifestApi)
{
    public Task<PagedResult<ManifestListItemDto>> SearchAsync(
        SearchManifestRequest request, string authorization, CancellationToken cancellationToken)
        => manifestApi.SearchAsync(request, authorization, cancellationToken);

    public Task<List<GetOrderInManifestResponse>> GetOrdersInManifestAsync(
        Guid manifestId, string authorization, CancellationToken cancellationToken)
        => manifestApi.GetOrdersInManifestAsync(manifestId, authorization, null, cancellationToken);

    public Task<GetManifestOrderDetailResponse?> GetManifestOrderDetailAsync(
        Guid manifestId, string orderId, string authorization, CancellationToken cancellationToken)
        => manifestApi.GetManifestOrderDetailAsync(manifestId, orderId, authorization, cancellationToken);
}
