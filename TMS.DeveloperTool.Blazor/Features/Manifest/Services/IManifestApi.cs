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
}
