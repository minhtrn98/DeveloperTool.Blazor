using TMS.DeveloperTool.Blazor.Database;
using DriverRecord = (System.Guid Id, string Name);

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class DriverService
{
    private readonly ApplicationDbQuery _dbQuery;

    public DriverService([FromKeyedServices("DriverDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<Dictionary<Guid, string>> GetDriverNamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, name
            FROM public.employees
            WHERE id = ANY(@Ids)
        ";
        IEnumerable<DriverRecord> drivers = await _dbQuery.QueryAsync<DriverRecord>(sql, new { Ids = ids }, cancellationToken);
        return drivers.ToDictionary(d => d.Id, d => d.Name);
    }
}
