using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyDriverService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OrderRepository _orderRepository;
    private readonly DriverRepository _driverRepository;
    private readonly JwtTokenService _jwtTokenService;

    public MyDriverService(
        ApplicationDbContext dbContext,
        OrderRepository orderRepository,
        DriverRepository driverRepository,
        JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
        _driverRepository = driverRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<int> SyncMissingDriversFromPickupTaskAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> pickupTaskAssignedDriverIds = (await _orderRepository.GetPickupTaskAssignedDriverIdsAsync(cancellationToken))
            .Distinct()
            .ToList();
        if (pickupTaskAssignedDriverIds.Count == 0)
        {
            return 0;
        }

        int insertedCount = 0;

        foreach (Guid[] batchIds in pickupTaskAssignedDriverIds.Chunk(50))
        {
            HashSet<Guid> existingDriverIds = await _dbContext.Drivers
                .Where(d => batchIds.Contains(d.DriverId))
                .Select(d => d.DriverId)
                .ToHashSetAsync(cancellationToken);

            List<Driver> newDrivers = [];

            foreach (Guid driverId in batchIds)
            {
                if (existingDriverIds.Contains(driverId))
                {
                    continue;
                }

                (Guid Id, string Name, string Code)? sourceDriver = await _driverRepository.GetDriverByIdAsync(driverId, cancellationToken);
                if (sourceDriver is null)
                {
                    continue;
                }

                newDrivers.Add(new Driver
                {
                    DriverId = sourceDriver.Value.Id,
                    Name = sourceDriver.Value.Name,
                    Code = sourceDriver.Value.Code,
                    Email = string.Empty,
                    BearerToken = string.Empty,
                    TokenExpiredAt = null
                });
            }

            if (newDrivers.Count == 0)
            {
                continue;
            }

            await _dbContext.Drivers.AddRangeAsync(newDrivers, cancellationToken);
            insertedCount += await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
        }

        return insertedCount;
    }

    public async Task<int> GenerateTokensForAllDriversAsync(CancellationToken cancellationToken = default)
    {
        List<string> permissions = await _driverRepository.GetAllPermissionsAsync(cancellationToken);
        List<Driver> drivers = await _dbContext.Drivers.ToListAsync(cancellationToken);
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        foreach (Driver driver in drivers)
        {
            bool hasValidToken = !string.IsNullOrWhiteSpace(driver.BearerToken)
                && driver.TokenExpiredAt.HasValue
                && driver.TokenExpiredAt.Value > utcNow;

            if (hasValidToken)
            {
                continue;
            }

            driver.BearerToken = _jwtTokenService.CreateToken(driver, permissions);
            driver.TokenExpiredAt = _jwtTokenService.GetDefaultExpiresAtUtc(utcNow);
        }

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Driver>> GetAllDriversAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Drivers.AsNoTracking().ToListAsync(cancellationToken);
    }
}
