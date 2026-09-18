using DriverRecord = (System.Guid Id, string Name, string Code);

namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class DriverRepository([FromKeyedServices("DriverDb")] ApplicationDbQuery dbQuery)
{
    public async Task<Dictionary<Guid, string>> GetDriverNamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, name, code
            FROM public.employees
            WHERE id = ANY(@Ids) and is_active = true
        ";
        IEnumerable<DriverRecord> drivers = await dbQuery.QueryAsync<DriverRecord>(sql, new { Ids = ids }, cancellationToken);
        return drivers.ToDictionary(d => d.Id, d => d.Name);
    }

    public async Task<EmployeeDto?> GetEmployeeByLicenseNoAsync(string licenseNo, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id as "Id", name as "Name", code as "Code"
            FROM public.employees
            WHERE license_no = @LicenseNo and is_active = true
        """;
        EmployeeDto? driver = await dbQuery.FirstOrDefaultAsync<EmployeeDto>(sql, new { LicenseNo = licenseNo }, cancellationToken);
        return driver;
    }

    public async Task<List<string>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT key as "Value"
            FROM public.app_permissions
        """;
        IEnumerable<string> permissions = await dbQuery.QueryAsync<string>(sql, null, cancellationToken);
        return [.. permissions];
    }
}
