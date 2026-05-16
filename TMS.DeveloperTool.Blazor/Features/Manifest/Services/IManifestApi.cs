using Refit;
using TMS.DeveloperTool.Blazor.Features.Manifest.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.Manifest.Services;

public interface IManifestApi
{
    [Post("/api/v1/delivery-manifests/commit")]
    Task<CommitDeliveryManifestResult> CommitAsync(
        [Body] CommitDeliveryManifestRequest request,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    [Post("/api/v1/delivery-manifests/search")]
    Task<PagedResult<ManifestListItemDto>> SearchAsync(
        [Body] SearchManifestRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/delivery-manifests/{manifestId}/search")]
    Task<List<GetOrderInManifestResponse>> GetOrdersInManifestAsync(
        Guid manifestId,
        [Query] string? searchTerm = null,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/delivery-manifests/{manifestId}/{orderId}/detail")]
    Task<GetManifestOrderDetailResponse?> GetManifestOrderDetailAsync(
        Guid manifestId,
        string orderId,
        CancellationToken cancellationToken = default);
}
