using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyEmployeeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OrderRepository _orderRepository;
    private readonly DriverRepository _driverRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly HashSet<string> _adminRoles = new(StringComparer.OrdinalIgnoreCase) { "SA", "PM" };

    public MyEmployeeService(
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

    public async Task<int> SyncMissingEmployeesFromPickupTaskAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> pickupTaskAssignedEmployeeIds = (await _orderRepository.GetPickupTaskAssignedDriverIdsAsync(cancellationToken))
            .Where(id => id != Guid.Empty)
            .ToList();
        if (pickupTaskAssignedEmployeeIds.Count == 0)
        {
            return 0;
        }

        int insertedCount = 0;

        foreach (Guid[] batchIds in pickupTaskAssignedEmployeeIds.Chunk(50))
        {
            HashSet<Guid> existingEmployeeIds = await _dbContext.Employees
                .Where(d => batchIds.Contains(d.EmployeeId))
                .Select(d => d.EmployeeId)
                .ToHashSetAsync(cancellationToken);

            List<Employee> newEmployees = [];

            foreach (Guid employeeId in batchIds)
            {
                if (existingEmployeeIds.Contains(employeeId))
                {
                    continue;
                }

                (Guid Id, string Name, string Code)? sourceEmployee = await _driverRepository.GetDriverByIdAsync(employeeId, cancellationToken);
                if (sourceEmployee is null)
                {
                    continue;
                }

                newEmployees.Add(new Employee
                {
                    EmployeeId = sourceEmployee.Value.Id,
                    Name = sourceEmployee.Value.Name,
                    Code = sourceEmployee.Value.Code,
                    BearerToken = string.Empty,
                    TokenExpiredAt = null
                });
            }

            if (newEmployees.Count == 0)
            {
                continue;
            }

            await _dbContext.Employees.AddRangeAsync(newEmployees, cancellationToken);
            insertedCount += await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
        }

        return insertedCount;
    }

    public async Task<int> GenerateTokensForAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        List<Employee> employees = await _dbContext.Employees.ToListAsync(cancellationToken);
        List<Guid> employeeIds = employees.Select(e => e.EmployeeId).ToList();

        Dictionary<Guid, EmployeeContactDto> empContactInfos = await _driverRepository.GetEmpContactAsync(employeeIds, cancellationToken);
        Dictionary<Guid, RoleDto[]> empRoles = await _driverRepository.GetEmpRolesAsync(employeeIds, cancellationToken);
        Dictionary<Guid, string[]> empDepts = await _driverRepository.GetEmpDeptsAsync(employeeIds, cancellationToken);
        Dictionary<Guid, string[]> rolePermissions = await _driverRepository.GetAllRolePermissionsAsync(cancellationToken);

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        foreach (Employee emp in employees)
        {
            bool hasValidToken = !string.IsNullOrWhiteSpace(emp.BearerToken)
                && emp.TokenExpiredAt.HasValue
                && emp.TokenExpiredAt.Value > utcNow;

            if (hasValidToken)
            {
                continue;
            }

            EmployeeContactDto? contactInfo = empContactInfos.GetValueOrDefault(emp.EmployeeId);
            if (contactInfo != null)
            {
                emp.Email = contactInfo.Email;
                emp.Phone = contactInfo.Phone;
            }

            EmployeePermissionInfo permissionInfo = await GetEmployeePermissionInfo(
                emp.EmployeeId,
                empRoles,
                empDepts,
                rolePermissions,
                cancellationToken
            );
            emp.BearerToken = _jwtTokenService.CreateToken(
                emp,
                permissionInfo.IsAdmin,
                permissionInfo.Permissions,
                permissionInfo.RoleNames,
                permissionInfo.AccessDepartments,
                permissionInfo.Version
            );
            emp.TokenExpiredAt = _jwtTokenService.GetDefaultExpiresAtUtc(utcNow);
        }

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddEmployeeAsync(string code, CancellationToken cancellationToken = default)
    {
        bool alreadyExists = await _dbContext.Employees.AnyAsync(d => d.Code == code, cancellationToken);
        if (alreadyExists)
        {
            return false;
        }

        EmployeeDto? employee = await _driverRepository.GetEmployeeByCodeAsync(code, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        Employee newEmployee = new()
        {
            EmployeeId = employee.Id,
            Name = employee.Name,
            Code = employee.Code,
            BearerToken = string.Empty,
            TokenExpiredAt = null
        };
        await _dbContext.Employees.AddAsync(newEmployee, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<EmployeePermissionInfo> GetEmployeePermissionInfo(Guid employeeId, Dictionary<Guid, RoleDto[]> empRoles, Dictionary<Guid, string[]> empDepts, Dictionary<Guid, string[]> rolePermissions, CancellationToken cancellationToken = default)
    {
        RoleDto[] empRolesForEmployee = empRoles.GetValueOrDefault(employeeId, []);
        List<string> roleNames = empRolesForEmployee.Select(r => r.Name).ToList();
        long version = empRolesForEmployee.Select(r => r.PermissionsVersion).DefaultIfEmpty(0).Max();
        bool isAdmin = empRolesForEmployee.Any(r => _adminRoles.Contains(r.Name, StringComparer.OrdinalIgnoreCase));
        List<string> permissions = empRolesForEmployee
            .SelectMany(r => rolePermissions.GetValueOrDefault(r.Id, []))
            .Distinct()
            .ToList();
        List<string> accessDepartments = empDepts.GetValueOrDefault(employeeId, []).ToList();

        // do later: implement real permission logic based on employeeId
        return new EmployeePermissionInfo(
            isAdmin: isAdmin,
            permissions: permissions,
            roleNames: roleNames,
            accessDepartments: accessDepartments,
            version: version
        );
    }
}

public sealed class EmployeePermissionInfo
{
    public EmployeePermissionInfo(bool isAdmin, List<string> permissions, List<string> roleNames, List<string> accessDepartments, long version)
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