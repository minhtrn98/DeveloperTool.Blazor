using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyDriverService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OrderRepository _orderRepository;
    private readonly DriverRepository _driverRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly HashSet<string> _adminRoles = new(StringComparer.OrdinalIgnoreCase) { "SA", "PM" };

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
            .Where(id => id != Guid.Empty)
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
        List<Driver> drivers = await _dbContext.Drivers.ToListAsync(cancellationToken);
        List<Guid> driverIds = drivers.Select(d => d.DriverId).ToList();

        Dictionary<Guid, EmployeeContactDto> empContactInfos = await _driverRepository.GetEmpContactAsync(driverIds, cancellationToken);
        Dictionary<Guid, RoleDto[]> empRoles = await _driverRepository.GetEmpRolesAsync(driverIds, cancellationToken);
        Dictionary<Guid, string[]> empDepts = await _driverRepository.GetEmpDeptsAsync(driverIds, cancellationToken);
        Dictionary<Guid, string[]> rolePermissions = await _driverRepository.GetAllRolePermissionsAsync(cancellationToken);

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

            EmployeeContactDto? contactInfo = empContactInfos.GetValueOrDefault(driver.DriverId);
            if (contactInfo != null)
            {
                driver.Email = contactInfo.Email;
                driver.Phone = contactInfo.Phone;
            }

            DriverPermissionInfo permissionInfo = await GetDriverPermissionInfo(
                driver.DriverId,
                empRoles,
                empDepts,
                rolePermissions,
                cancellationToken
            );
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

    public async Task<DriverPermissionInfo> GetDriverPermissionInfo(Guid driverId, Dictionary<Guid, RoleDto[]> empRoles, Dictionary<Guid, string[]> empDepts, Dictionary<Guid, string[]> rolePermissions, CancellationToken cancellationToken = default)
    {
        RoleDto[] empRolesForDriver = empRoles.GetValueOrDefault(driverId, []);
        List<string> roleNames = empRolesForDriver.Select(r => r.Name).ToList();
        long version = empRolesForDriver.Select(r => r.PermissionsVersion).DefaultIfEmpty(0).Max();
        bool isAdmin = empRolesForDriver.Any(r => _adminRoles.Contains(r.Name, StringComparer.OrdinalIgnoreCase));
        List<string> permissions = empRolesForDriver
            .SelectMany(r => rolePermissions.GetValueOrDefault(r.Id, []))
            .Distinct()
            .ToList();
        List<string> accessDepartments = empDepts.GetValueOrDefault(driverId, []).ToList();

        // do later: implement real permission logic based on driverId
        return new DriverPermissionInfo(
            isAdmin: isAdmin,
            permissions: permissions,
            roleNames: roleNames,
            accessDepartments: accessDepartments,
            version: version
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