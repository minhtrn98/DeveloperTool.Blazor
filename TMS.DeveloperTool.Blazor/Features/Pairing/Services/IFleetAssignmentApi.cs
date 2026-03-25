using Refit;
using TMS.DeveloperTool.Blazor.Features.Pairing.Contracts;
using TMS.DeveloperTool.Blazor.Features.Pairing.Models;

namespace TMS.DeveloperTool.Blazor.Features.Pairing.Services;

public interface IFleetAssignmentApi
{
    [Post("/api/v1/assignments/pairing")]
    Task PairingAsync([Body] PairingRequest request, [Header("Authorization")] string? authorization, [Header("CompanyId")] string? companyId, [Header("DepartmentId")] string? departmentId, CancellationToken cancellationToken = default);

    [Post("/api/v1/assignments/unpairing")]
    Task UnpairingAsync([Body] PairingRequest request, [Header("Authorization")] string? authorization, [Header("CompanyId")] string? companyId, [Header("DepartmentId")] string? departmentId, CancellationToken cancellationToken = default);

    [Post("/api/v1/assignments/unexpected")]
    Task UnexpectedPairingAsync([Body] PairingRequest request, [Header("Authorization")] string? authorization, [Header("CompanyId")] string? companyId, [Header("DepartmentId")] string? departmentId, CancellationToken cancellationToken = default);

    [Post("/api/v1/assignments/request")]
    Task SwapDriverAsync([Body] SwapDriverRequest request, [Header("Authorization")] string? authorization, [Header("CompanyId")] string? companyId, [Header("DepartmentId")] string? departmentId, CancellationToken cancellationToken = default);

    [Post("/api/v1/assignments/confirm")]
    Task ConfirmAsync([Body] ConfirmRequest request, [Header("Authorization")] string? authorization, [Header("CompanyId")] string? companyId, [Header("DepartmentId")] string? departmentId, CancellationToken cancellationToken = default);
}
