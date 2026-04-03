using System.Text.Json;
using TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Services;

public sealed class PickupTaskActionService(IPickupTaskApi api)
{
    public Task AcceptAsync(string pickupTaskId, string bearerToken, CancellationToken cancellationToken = default)
        => api.AcceptAsync(pickupTaskId, $"Bearer {bearerToken}", cancellationToken);

    public Task ConfirmArrivedAsync(string pickupTaskId, string bearerToken, CancellationToken cancellationToken = default)
        => api.ConfirmArrivedAsync(new ConfirmArrivalRequest([pickupTaskId]), $"Bearer {bearerToken}", cancellationToken);

    public Task DriverCancelAsync(
        string pickupTaskId,
        string bearerToken,
        string reason,
        string[] orderItemIds,
        CancellationToken cancellationToken = default)
        => api.DriverCancelAsync(new DriverCancelPickupTaskRequest(pickupTaskId, reason, orderItemIds), $"Bearer {bearerToken}", cancellationToken);

    public Task RescheduleAsync(
        string pickupTaskId,
        string bearerToken,
        DateTime rescheduledPickupDt,
        string reason,
        string[] orderItemIds,
        CancellationToken cancellationToken = default)
        => api.RescheduleAsync(
            new ReschedulePickupTaskRequest(pickupTaskId, rescheduledPickupDt.ToUniversalTime(), reason, orderItemIds),
            $"Bearer {bearerToken}",
            cancellationToken);

    public Task TransferAsync(
        string pickupTaskId,
        string bearerToken,
        string reason,
        string? targetDriverId,
        CancellationToken cancellationToken = default)
        => api.TransferAsync(
            new TransferPickupTaskRequest(pickupTaskId, reason, targetDriverId),
            $"Bearer {bearerToken}",
            cancellationToken);

    public Task ConfirmPickupAsync(
        ConfirmPickupRequest request,
        string bearerToken,
        CancellationToken cancellationToken = default)
        => api.ConfirmPickupAsync(request, $"Bearer {bearerToken}", cancellationToken);

    public Task CompletePickupAsync(string pickupTaskId, string bearerToken, CancellationToken cancellationToken = default)
        => api.CompletePickupAsync(new CompletePickupRequest(pickupTaskId), $"Bearer {bearerToken}", cancellationToken);

    public Task<JsonElement> GetCompletePreparationAsync(string pickupTaskId, string bearerToken, CancellationToken cancellationToken = default)
        => api.GetCompletePreparationAsync(pickupTaskId, $"Bearer {bearerToken}", cancellationToken);
}
