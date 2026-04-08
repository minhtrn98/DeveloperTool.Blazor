using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyDriverService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OrderRepository _orderRepository;
    private readonly DriverRepository _driverRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IWebHostEnvironment _environment;
    private readonly HashSet<string> _adminRoles = new (StringComparer.OrdinalIgnoreCase) { "SA", "PM" };

    public MyDriverService(
        ApplicationDbContext dbContext,
        OrderRepository orderRepository,
        DriverRepository driverRepository,
        JwtTokenService jwtTokenService,
        IWebHostEnvironment webHostEnvironment)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
        _driverRepository = driverRepository;
        _jwtTokenService = jwtTokenService;
        _environment = webHostEnvironment;
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
        List<string> allPermissions = await _driverRepository.GetAllPermissionsAsync(cancellationToken);
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

            DriverPermissionInfo permissionInfo = await GetDriverPermissionInfo(driver.DriverId, allPermissions, cancellationToken);
            driver.BearerToken = _jwtTokenService.CreateToken(
                driver,
                permissionInfo.IsAdmin,
                permissionInfo.Permissions,
                permissionInfo.RoleNames,
                permissionInfo.AccessDepartments,
                permissionInfo.Version
            );
            driver.TokenExpiredAt = _jwtTokenService.GetDefaultExpiresAtUtc(utcNow);
        }

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddDriverAsync(string code, CancellationToken cancellationToken = default)
    {
        bool alreadyExists = await _dbContext.Drivers.AnyAsync(d => d.Code == code, cancellationToken);
        if (alreadyExists)
        {
            return false;
        }

        EmployeeDto? employee = await _driverRepository.GetEmployeeByCodeAsync(code, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        Driver newDriver = new()
        {
            DriverId = employee.Id,
            Name = employee.Name,
            Code = employee.Code,
            BearerToken = string.Empty,
            TokenExpiredAt = null
        };
        await _dbContext.Drivers.AddAsync(newDriver, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<Driver>> GetAllDriversAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Drivers.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<DriverPermissionInfo> GetDriverPermissionInfo(Guid driverId, List<string> allPermissions, CancellationToken cancellationToken = default)
    {
        if (_environment.IsLocal())
        {
            return new DriverPermissionInfo(
                isAdmin: true,
                permissions: allPermissions,
                roleNames: ["FullAccess"],
                accessDepartments: ["*"],
                version: 1
            );
        }



        // do later: implement real permission logic based on driverId
        return new DriverPermissionInfo(
            isAdmin: true,
            permissions: allPermissions,
            roleNames: ["FullAccess"],
            accessDepartments: ["*"],
            version: 1
        );
    }
}

public sealed class DriverPermissionInfo
{
    public DriverPermissionInfo(bool isAdmin, List<string> permissions, List<string> roleNames, List<string> accessDepartments, long version)
    {
        IsAdmin = isAdmin;
        Permissions = permissions;
        RoleNames = roleNames;
        AccessDepartments = isAdmin ? ["*"] : accessDepartments; // Nếu là admin, cấp quyền truy cập tất cả phòng ban
        Version = version;
    }

    public bool IsAdmin { get; init; }
    public List<string> Permissions { get; init; }
    public List<string> RoleNames { get; init; }
    public List<string> AccessDepartments { get; init; } = [];
    public long Version { get; init; }
}