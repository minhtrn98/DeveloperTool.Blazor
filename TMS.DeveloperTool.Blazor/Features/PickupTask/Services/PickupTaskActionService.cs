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
        string[] orderIds,
        CancellationToken cancellationToken = default)
        => api.DriverCancelAsync(new DriverCancelPickupTaskRequest(pickupTaskId, reason, orderIds), $"Bearer {bearerToken}", cancellationToken);

    public Task RescheduleAsync(
        string pickupTaskId,
        string bearerToken,
        DateTime rescheduledPickupDt,
        string reason,
        string[] orderIds,
        CancellationToken cancellationToken = default)
        => api.RescheduleAsync(
            new ReschedulePickupTaskRequest(pickupTaskId, rescheduledPickupDt.ToUniversalTime(), reason, orderIds),
            $"Bearer {bearerToken}",
            cancellationToken);
}

public sealed record DriverCancelPickupTaskRequest(
    string PickupTaskId,
    string Reason,
    string[] OrderIds);

public sealed record ConfirmArrivalRequest(List<string> PickupTaskIds);

public sealed record ReschedulePickupTaskRequest(
    string PickupTaskId,
    DateTime RescheduledPickupDt,
    string Reason,
    string[] OrderIds);
