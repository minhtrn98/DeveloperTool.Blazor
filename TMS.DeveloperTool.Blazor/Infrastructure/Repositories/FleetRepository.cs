namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class FleetRepository([FromKeyedServices("FleetDb")] ApplicationDbQuery dbQuery)
{
    public async Task<List<AssignmentLookupDto>> GetAssignmentsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select a."Id", a."Code"
            from public."Assignments" a
            where a."Id" = ANY(@Ids)
            order by a."Code"
        """;
        IEnumerable<AssignmentLookupDto> assignments = await dbQuery.QueryAsync<AssignmentLookupDto>(sql, new { Ids = ids }, cancellationToken);
        return assignments.ToList();
    }

    public async Task<List<VehicleLookupDto>> GetVehiclesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select v."Id", v."ActualPlate"
            from public."Vehicles" v
            where v."Id" = ANY(@Ids)
            order by v."ActualPlate"
        """;
        IEnumerable<VehicleLookupDto> vehicles = await dbQuery.QueryAsync<VehicleLookupDto>(sql, new { Ids = ids }, cancellationToken);
        return vehicles.ToList();
    }
}
