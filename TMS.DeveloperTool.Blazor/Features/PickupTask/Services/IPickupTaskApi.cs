using Refit;
using TMS.DeveloperTool.Blazor.Features.PickupTask.Contracts;

namespace TMS.DeveloperTool.Blazor.Features.PickupTask.Services;

public interface IPickupTaskApi
{
    [Put("/api/v1/pickup-tasks/accept")]
    Task AcceptAsync([Query] string pickupTaskId, [Header("Authorization")] string authorization, CancellationToken cancellationToken = default);

    [Post("/api/v1/pickup-tasks/confirm-arrived")]
    Task ConfirmArrivedAsync([Body] ConfirmArrivalRequest request, [Header("Authorization")] string authorization, CancellationToken cancellationToken = default);

    [Put("/api/v1/pickup-tasks/driver-cancel")]
    Task DriverCancelAsync([Body] DriverCancelPickupTaskRequest request, [Header("Authorization")] string authorization, CancellationToken cancellationToken = default);

    [Put("/api/v1/pickup-tasks/reschedule")]
    Task RescheduleAsync([Body] ReschedulePickupTaskRequest request, [Header("Authorization")] string authorization, CancellationToken cancellationToken = default);

    [Post("/api/v1/pickup-tasks/transfer")]
    Task TransferAsync([Body] TransferPickupTaskRequest request, [Header("Authorization")] string authorization, CancellationToken cancellationToken = default);

    [Post("/api/v1/pickup-tasks/confirm-pickup")]
    Task ConfirmPickupAsync([Body] ConfirmPickupRequest request, [Header("Authorization")] string authorization, CancellationToken cancellationToken = default);
}
