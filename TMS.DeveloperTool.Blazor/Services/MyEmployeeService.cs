using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyEmployeeService(
    ApplicationDbContext dbContext,
    OrderRepository orderRepository,
    DriverRepository driverRepository,
    JwtTokenService jwtTokenService)
{
    private readonly HashSet<string> _adminRoles = new(StringComparer.OrdinalIgnoreCase) { "SA", "PM" };

    public async Task<int> SyncMissingEmployeesFromPickupTaskAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> pickupTaskAssignedEmployeeIds = [.. (await orderRepository.GetPickupTaskAssignedDriverIdsAsync(cancellationToken)).Where(id => id != Guid.Empty)];
        if (pickupTaskAssignedEmployeeIds.Count == 0)
        {
            return 0;
        }

        int insertedCount = 0;

        foreach (Guid[] batchIds in pickupTaskAssignedEmployeeIds.Chunk(50))
        {
            HashSet<Guid> existingEmployeeIds = await dbContext.Employees
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

                (Guid Id, string Name, string Code)? sourceEmployee = await driverRepository.GetDriverByIdAsync(employeeId, cancellationToken);
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

            await dbContext.Employees.AddRangeAsync(newEmployees, cancellationToken);
            insertedCount += await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
        }

        return insertedCount;
    }

    public async Task<int> GenerateTokensForAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        List<Employee> employees = await dbContext.Employees.ToListAsync(cancellationToken);
        List<Guid> employeeIds = [.. employees.Select(e => e.EmployeeId)];

        Dictionary<Guid, EmployeeContactDto> empContactInfos = await driverRepository.GetEmployeeContactsAsync(employeeIds, cancellationToken);
        Dictionary<Guid, RoleDto[]> empRoles = await driverRepository.GetEmployeeRolesAsync(employeeIds, cancellationToken);
        Dictionary<Guid, string[]> empDepts = await driverRepository.GetEmployeeDepartmentsAsync(employeeIds, cancellationToken);
        Dictionary<Guid, string[]> rolePermissions = await driverRepository.GetAllRolePermissionsAsync(cancellationToken);

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
            emp.BearerToken = jwtTokenService.CreateToken(
                emp,
                permissionInfo.IsAdmin,
                permissionInfo.Permissions,
                permissionInfo.RoleNames,
                permissionInfo.AccessDepartments,
                permissionInfo.Version
            );
            emp.TokenExpiredAt = jwtTokenService.GetDefaultExpiresAtUtc(utcNow);
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddEmployeeAsync(string code, CancellationToken cancellationToken = default)
    {
        bool alreadyExists = await dbContext.Employees.AnyAsync(d => d.Code == code, cancellationToken);
        if (alreadyExists)
        {
            return false;
        }

        EmployeeDto? employee = await driverRepository.GetEmployeeByCodeAsync(code, cancellationToken);
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
        await dbContext.Employees.AddAsync(newEmployee, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<EmployeePermissionInfo> GetEmployeePermissionInfo(Guid employeeId, Dictionary<Guid, RoleDto[]> empRoles, Dictionary<Guid, string[]> empDepts, Dictionary<Guid, string[]> rolePermissions, CancellationToken cancellationToken = default)
    {
        RoleDto[] empRolesForEmployee = empRoles.GetValueOrDefault(employeeId, []);
        List<string> roleNames = [.. empRolesForEmployee.Select(r => r.Name)];
        long version = empRolesForEmployee.Select(r => r.PermissionsVersion).DefaultIfEmpty(0).Max();
        bool isAdmin = empRolesForEmployee.Any(r => _adminRoles.Contains(r.Name, StringComparer.OrdinalIgnoreCase));
        List<string> permissions = [.. empRolesForEmployee
            .SelectMany(r => rolePermissions.GetValueOrDefault(r.Id, []))
            .Distinct()];
        List<string> accessDepartments = [.. empDepts.GetValueOrDefault(employeeId, [])];

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

public sealed class EmployeePermissionInfo(bool isAdmin, List<string> permissions, List<string> roleNames, List<string> accessDepartments, long version)
{
    public bool IsAdmin { get; init; } = isAdmin;
    public List<string> Permissions { get; init; } = permissions;
    public List<string> RoleNames { get; init; } = roleNames;
    public List<string> AccessDepartments { get; init; } = isAdmin ? ["*"] : accessDepartments; // Nếu là admin, cấp quyền truy cập tất cả phòng ban
    public long Version { get; init; } = version;
}