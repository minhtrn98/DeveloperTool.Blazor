using DriverDeptRecord = (System.Guid DriverId, string DeptCode);
using DriverRecord = (System.Guid Id, string Name, string Code);
using DriverRoleRecord = (System.Guid DriverId, System.Guid RoleId, string RoleName, long PermissionsVersion);
using RolePermissionRecord = (System.Guid RoleId, string PermissionKey);

namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class DriverRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public DriverRepository([FromKeyedServices("DriverDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<Dictionary<Guid, string>> GetDriverNamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, name, code
            FROM public.employees
            WHERE id = ANY(@Ids) and is_active = true
        ";
        IEnumerable<DriverRecord> drivers = await _dbQuery.QueryAsync<DriverRecord>(sql, new { Ids = ids }, cancellationToken);
        return drivers.ToDictionary(d => d.Id, d => d.Name);
    }

    public async Task<Dictionary<Guid, EmployeeContactDto>> GetEmployeeContactsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id as "Id", email as "Email", phone as "Phone"
            FROM public.employees
            WHERE id = ANY(@Ids) and is_active = true
        """;
        IEnumerable<EmployeeContactDto> contacts = await _dbQuery.QueryAsync<EmployeeContactDto>(sql, new { Ids = ids }, cancellationToken);
        return contacts.ToDictionary(d => d.Id, d => d);
    }

    public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string code, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id as "Id", name as "Name", code as "Code"
            FROM public.employees
            WHERE code = @Code and is_active = true
        """;
        EmployeeDto? driver = await _dbQuery.FirstOrDefaultAsync<EmployeeDto>(sql, new { Code = code }, cancellationToken);
        return driver;
    }

    public async Task<EmployeeDto?> GetEmployeeByLicenseNoAsync(string licenseNo, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id as "Id", name as "Name", code as "Code"
            FROM public.employees
            WHERE license_no = @LicenseNo and is_active = true
        """;
        EmployeeDto? driver = await _dbQuery.FirstOrDefaultAsync<EmployeeDto>(sql, new { LicenseNo = licenseNo }, cancellationToken);
        return driver;
    }

    public async Task<Dictionary<Guid, RoleDto[]>> GetEmployeeRolesAsync(IEnumerable<Guid> driverIds, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT e.id as "DriverId", r.id as "RoleId", r.name as "RoleName", r.permissions_version as "PermissionsVersion"
            FROM public.employees e
            JOIN public.app_user_roles er ON e.id = er.employee_id
            JOIN public.app_roles r ON er.role_id = r.id
            WHERE e.id = ANY(@DriverIds) and e.is_active = true
        """;
        IEnumerable<DriverRoleRecord> driverRoles = await _dbQuery.QueryAsync<DriverRoleRecord>(sql, new { DriverIds = driverIds }, cancellationToken);
        return driverRoles
            .GroupBy(dr => dr.DriverId)
            .ToDictionary(g => g.Key, g => g.Select(dr => new RoleDto(dr.RoleId, dr.RoleName, dr.PermissionsVersion)).ToArray());
    }

    public async Task<Dictionary<Guid, string[]>> GetEmployeeDepartmentsAsync(IEnumerable<Guid> driverIds, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT e.id as "DriverId", d.code as "DeptCode"
            FROM public.employees e
            JOIN public.app_user_departments ud ON e.id = ud.employee_id
            JOIN public.departments d ON ud.department_id = d.id
            WHERE e.id = ANY(@DriverIds) and e.is_active = true
        """;
        IEnumerable<DriverDeptRecord> driverDepts = await _dbQuery.QueryAsync<DriverDeptRecord>(sql, new { DriverIds = driverIds }, cancellationToken);
        return driverDepts
            .GroupBy(dr => dr.DriverId)
            .ToDictionary(g => g.Key, g => g.Select(dr => dr.DeptCode).ToArray());
    }

    public async Task<Dictionary<Guid, string[]>> GetAllRolePermissionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select r.id as "RoleId", p.key as "PermissionKey"
            from public.app_roles r
            join public.app_role_permissions rp on r.id = rp.role_id
            join public.app_permissions p on rp.permission_id = p.id
        """;
        IEnumerable<RolePermissionRecord> rolePermissions = await _dbQuery.QueryAsync<RolePermissionRecord>(sql, null, cancellationToken);
        return rolePermissions
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(rp => rp.PermissionKey).ToArray());
    }

    public async Task<DriverRecord?> GetDriverByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, name, code
            FROM public.employees
            WHERE id = @Id
        ";
        DriverRecord? driver = await _dbQuery.FirstOrDefaultAsync<DriverRecord>(sql, new { Id = id }, cancellationToken);
        return driver;
    }

    public async Task<List<string>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT key as "Value"
            FROM public.app_permissions
        """;
        IEnumerable<string> permissions = await _dbQuery.QueryAsync<string>(sql, null, cancellationToken);
        return permissions.ToList();
    }
}
