using VehicleRecord = (System.Guid Id, string LicensePlate);

namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class FleetRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public FleetRepository([FromKeyedServices("FleetDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<Dictionary<Guid, string>> GetVehiclePlateAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
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
