using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Features.Simulation.Models;

namespace TMS.DeveloperTool.Blazor.Features.Simulation.Services;

public sealed class FakeVehicleTransportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly EventService _eventService;
    private readonly ILogger<FakeVehicleTransportService> _logger;
    private readonly FleetRepository _fleetRepository;

    public FakeVehicleTransportService(ApplicationDbContext dbContext, ILogger<FakeVehicleTransportService> logger, EventService eventService, FleetRepository fleetRepository)
    {
        _dbContext = dbContext;
        _logger = logger;
        _eventService = eventService;
        _fleetRepository = fleetRepository;
    }

    public async Task StartAsync(string licensePlate, Guid templateId, CancellationToken cancellationToken = default)
    {
        string actualPlate = licensePlate.Replace("-", "").Replace(".", "");

        Vehicle? vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.LicensePlate == actualPlate, cancellationToken);

        if (vehicle?.IsMoving == true)
        {
            throw new InvalidOperationException("Vehicle is already moving.");
        }

        // get last odo from fleet service
        double lastOdo = await _fleetRepository.GetVehicleOdometerAsync(licensePlate, cancellationToken);

        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                LicensePlate = actualPlate,
                LastOdo = lastOdo,
                IsMoving = true
            };
            _dbContext.Vehicles.Add(vehicle);
        }
        else
        {
            vehicle.LastOdo = lastOdo;
            vehicle.IsMoving = true;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            RouteCheckPointTemplate template = await _dbContext.RouteCheckPointTemplates
                .AsNoTracking()
                .Include(x => x.RouteCheckPoints)
                .FirstAsync(x => x.Id == templateId, cancellationToken);
            List<RouteCheckPoint> checkPoints = template.RouteCheckPoints.OrderBy(x => x.Order).ToList();

            foreach (RouteCheckPoint checkPoint in checkPoints)
            {
                if (checkPoint.Km <= 0)
                    continue;

                vehicle.LastOdo += checkPoint.Km;

                // publish event rabbitmq to simulate gps update
                VehicleTrackingEvent vehicleTrackingData = new()
                {
                    Data = new VehicleTrackingData
                    {
                        ActualPlate = vehicle.LicensePlate,
                        LastOdoMile = vehicle.LastOdo,
                        Latitude = checkPoint.Lat,
                        Longitude = checkPoint.Lon,
                        TraceUrl = "",
                        TraceAddress = checkPoint.Address,
                        Heading = 0,
                        Speed = 1
                    }
                };
                await _eventService.PublishTrackingEvent(vehicleTrackingData, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(template.JumpSeconds), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while simulating vehicle transport.");
            throw;
        }
        finally
        {
            vehicle.IsMoving = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
