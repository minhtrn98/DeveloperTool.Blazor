using DriverRecord = (System.Guid Id, string Name, string Code);

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
