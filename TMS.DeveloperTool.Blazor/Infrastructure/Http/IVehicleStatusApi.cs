using Refit;
using TMS.DeveloperTool.Blazor.Features.Tracking.Dtos;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

public interface IVehicleStatusApi
{
    [Get("/v3/tracking/GetVehicleStatus")]
    Task<VehicleStatusResponseDto> GetVehicleStatusAsync([Query] string plate, CancellationToken cancellationToken = default);
}
