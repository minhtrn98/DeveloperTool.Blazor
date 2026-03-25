using Refit;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Features.Pairing.Contracts;
using TMS.DeveloperTool.Blazor.Features.Pairing.Models;

namespace TMS.DeveloperTool.Blazor.Features.Pairing.Services;

public sealed class PairingService(IFleetAssignmentApi api)
{
    public Task<ApiCallResult> SendPairingAsync(
        string accessToken,
        string? companyId,
        string? departmentId,
        PairingRequest request,
        CancellationToken cancellationToken = default)
    {
        (string? auth, string? companyHeader, string? deptHeader) = BuildHeaders(accessToken, companyId, departmentId);
        Task apiTask = request.ActionType switch
        {
            ActionType.Pairing => api.PairingAsync(request, auth, companyHeader, deptHeader, cancellationToken),
            ActionType.Unpairing => api.UnpairingAsync(request, auth, companyHeader, deptHeader, cancellationToken),
            ActionType.UnexpectedPairing => api.UnexpectedPairingAsync(request, auth, companyHeader, deptHeader, cancellationToken),
            _ => throw new NotSupportedException($"ActionType '{request.ActionType}' không được hỗ trợ.")
        };
        return WrapAsync(apiTask);
    }

    public Task<ApiCallResult> SendSwapDriverAsync(
        string accessToken,
        string? companyId,
        string? departmentId,
        SwapDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        (string? auth, string? companyHeader, string? deptHeader) = BuildHeaders(accessToken, companyId, departmentId);
        return WrapAsync(api.SwapDriverAsync(request, auth, companyHeader, deptHeader, cancellationToken));
    }

    public Task<ApiCallResult> SendConfirmAsync(
        string accessToken,
        string? companyId,
        string? departmentId,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        (string? auth, string? companyHeader, string? deptHeader) = BuildHeaders(accessToken, companyId, departmentId);
        return WrapAsync(api.ConfirmAsync(new ConfirmRequest { VehicleId = vehicleId }, auth, companyHeader, deptHeader, cancellationToken));
    }

    private static (string? auth, string? companyId, string? departmentId) BuildHeaders(
        string accessToken, string? companyId, string? departmentId)
    {
        string? auth = string.IsNullOrWhiteSpace(accessToken) ? null : $"Bearer {accessToken.Trim()}";
        string? companyHeader = Guid.TryParse(companyId, out Guid companyGuid) ? companyGuid.ToString() : null;
        string? deptHeader = Guid.TryParse(departmentId, out Guid deptGuid) ? deptGuid.ToString() : null;
        return (auth, companyHeader, deptHeader);
    }

    private static async Task<ApiCallResult> WrapAsync(Task apiTask)
    {
        try
        {
            await apiTask;
            return ApiCallResult.Success();
        }
        catch (ApiException ex)
        {
            string message = $"{(int)ex.StatusCode} {ex.ReasonPhrase}. {ex.Content}";
            return ApiCallResult.Failure(message);
        }
    }
}

public sealed class ApiCallResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private ApiCallResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static ApiCallResult Success() => new(true, null);

    public static ApiCallResult Failure(string errorMessage) => new(false, errorMessage);
}
