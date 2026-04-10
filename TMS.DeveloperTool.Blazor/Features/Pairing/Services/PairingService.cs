using Refit;
using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Features.Pairing.Contracts;
using TMS.DeveloperTool.Blazor.Features.Pairing.Models;

namespace TMS.DeveloperTool.Blazor.Features.Pairing.Services;

public sealed class PairingService(IFleetAssignmentApi api)
{
    public Task<ApiCallResult> SendPairingAsync(
        string accessToken,
        PairingRequest request,
        CancellationToken cancellationToken = default)
    {
        string? auth = BuildAuthorizationHeader(accessToken);
        Task apiTask = request.ActionType switch
        {
            ActionType.Pairing => api.PairingAsync(request, auth, cancellationToken),
            ActionType.Unpairing => api.UnpairingAsync(request, auth, cancellationToken),
            ActionType.UnexpectedPairing => api.UnexpectedPairingAsync(request, auth, cancellationToken),
            _ => throw new NotSupportedException($"ActionType '{request.ActionType}' không được hỗ trợ.")
        };
        return WrapAsync(apiTask);
    }

    public Task<ApiCallResult> SendSwapDriverAsync(
        string accessToken,
        SwapDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        string? auth = BuildAuthorizationHeader(accessToken);
        return WrapAsync(api.SwapDriverAsync(request, auth, cancellationToken));
    }

    public Task<ApiCallResult> SendConfirmAsync(
        string accessToken,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        string? auth = BuildAuthorizationHeader(accessToken);
        return WrapAsync(api.ConfirmAsync(new ConfirmRequest { VehicleId = vehicleId }, auth, cancellationToken));
    }

    private static string? BuildAuthorizationHeader(string accessToken)
    {
        return string.IsNullOrWhiteSpace(accessToken) ? null : $"Bearer {accessToken.Trim()}";
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
