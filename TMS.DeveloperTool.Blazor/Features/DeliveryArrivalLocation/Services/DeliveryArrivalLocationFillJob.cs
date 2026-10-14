using TMS.DeveloperTool.Blazor.Features.DeliveryArrivalLocation.Models;

namespace TMS.DeveloperTool.Blazor.Features.DeliveryArrivalLocation.Services;

/// <summary>
/// Every <see cref="DeliveryArrivalLocationFillOptions.IntervalMinutes"/>, fills the vehicle and
/// order positions of recent arrivals (see <see cref="DeliveryArrivalLocationFillService"/>).
/// Independent of <c>SignozProSyncJob</c>: whatever isn't synced yet is picked up on a later run.
/// </summary>
public sealed class DeliveryArrivalLocationFillJob(
    DeliveryArrivalLocationFillService fillService,
    DeliveryArrivalLocationFillOptions options,
    ILogger<DeliveryArrivalLocationFillJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.IntervalMinutes));
        do
        {
            // Same filter as the other jobs: only a host shutdown may end the loop.
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Delivery arrival location fill failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        DeliveryArrivalLocationFillResult result = await fillService.FillAsync(cancellationToken);

        logger.LogInformation(
            "Delivery arrival location fill since {Since} (UTC): {VehicleFilled} vehicle, {OrderFilled} order location(s) set.",
            result.SinceUtc, result.VehicleFilled, result.OrderFilled);
    }
}
