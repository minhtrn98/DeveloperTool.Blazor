using TMS.DeveloperTool.Blazor.Database;
using VehicleRecord = (System.Guid Id, string LicensePlate);

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class FleetService
{
    private readonly ApplicationDbQuery _dbQuery;

    public FleetService([FromKeyedServices("FleetDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<Dictionary<Guid, string>> GetVehiclePlateAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "LicensePlate"
            FROM public."Vehicles"
            WHERE "Id" = ANY(@Ids)
        """;
        IEnumerable<VehicleRecord> drivers = await _dbQuery.QueryAsync<VehicleRecord>(sql, new { Ids = ids }, cancellationToken);
        return drivers.ToDictionary(d => d.Id, d => d.LicensePlate);
    }
}
