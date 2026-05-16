using TMS.DeveloperTool.Blazor.Infrastructure.Http;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class VehicleStatusService(IVehicleStatusApi vehicleStatusApi, FleetRepository fleetRepository, DriverRepository driverRepository)
{
    public async Task<(List<VehicleStatusTrackingDto> Items, int TotalCount)> GetVehicleStatusesAsync(
        string? searchValue,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        List<VehicleStatusTrackingDto> statusTrackingDtos = [];

        int safePageIndex = pageIndex < 1 ? 1 : pageIndex;
        int safePageSize = pageSize < 1 ? 10 : pageSize;
        string? normalizedSearch = NormalizeLicensePlate(searchValue);

        // get all vehicles
        var (totalCount, vehiclePlates) = await fleetRepository.SearchVehicleWithPaging(safePageIndex, safePageSize, normalizedSearch, cancellationToken);

        // call api to get vehicle status tracking info
        foreach (VehicleDropdownItemDto vehicle in vehiclePlates)
        {
            string licensePlate = vehicle.LicensePlate.Replace("-", string.Empty).Replace(".", string.Empty);
            VehicleStatusResponseDto vehicleStatus = await vehicleStatusApi.GetVehicleStatusAsync(licensePlate, cancellationToken);
            string? licenseNo = vehicleStatus.Data.Count > 0
                ? vehicleStatus.Data[0].LicenseNo
                : null;
            EmployeeDto? emp = null;

            if (!string.IsNullOrEmpty(licenseNo))
            {
                // find driver by license no
                emp = await driverRepository.GetEmployeeByLicenseNoAsync(licenseNo, cancellationToken);
            }

            VehicleStatusTrackingDto trackingInfo = new()
            {
                VehicleId = vehicle.Id,
                Plate = vehicle.LicensePlate,
                VehicleDepartmentCode = vehicle.DeptManagerCode,
                ActualPlate = licensePlate,
                LicenseNo = licenseNo ?? string.Empty,
                DriverId = emp?.Id,
                DriverCode = emp?.Code,
                DriverName = emp?.Name
            };

            statusTrackingDtos.Add(trackingInfo);
        }

        return (statusTrackingDtos, totalCount);
    }

    private static string? NormalizeLicensePlate(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value
            .Replace("-", string.Empty)
            .Replace(".", string.Empty)
            .Trim()
            .ToLowerInvariant();
    }
}
