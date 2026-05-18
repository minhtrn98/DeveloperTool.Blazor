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
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/delivery-manifests/{manifestId}/search")]
    Task<List<GetOrderInManifestResponse>> GetOrdersInManifestAsync(
        Guid manifestId,
        [Header("Authorization")] string authorization,
        [Query] string? searchTerm = null,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/delivery-manifests/{manifestId}/{orderId}/detail")]
    Task<GetManifestOrderDetailResponse?> GetManifestOrderDetailAsync(
        Guid manifestId,
        string orderId,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken = default);
}
