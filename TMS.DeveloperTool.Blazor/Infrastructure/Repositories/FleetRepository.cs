using TMS.DeveloperTool.Blazor.Domain.Enums;
using TMS.DeveloperTool.Blazor.Extensions;
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
        IEnumerable<VehicleRecord> vehicles = await _dbQuery.QueryAsync<VehicleRecord>(sql, new { Ids = ids }, cancellationToken);
        return vehicles.ToDictionary(d => d.Id, d => d.LicensePlate);
    }

    public async Task<Dictionary<Guid, string>> GetAllVehiclePlateAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "LicensePlate"
            FROM public."Vehicles"
        """;
        IEnumerable<VehicleRecord> vehicles = await _dbQuery.QueryAsync<VehicleRecord>(sql, null, cancellationToken);
        return vehicles.ToDictionary(d => d.Id, d => d.LicensePlate);
    }

    public async Task<IEnumerable<Guid>> GetAllVehicleIdsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id"
            FROM public."Vehicles"
        """;
        return await _dbQuery.QueryAsync<Guid>(sql, null, cancellationToken);
    }

    public async Task<IEnumerable<DropdownItem<Guid>>> GetPairingReasonTypes(CancellationToken cancellationToken)
    {
        const string sql = """
            select  "Id" ,"Code" ,"Name"
            from public."PairingReasonTypes"
            where "IsDeleted" = false
        """;
        IEnumerable<DropdownItem<Guid>> records = await _dbQuery.QueryAsync<DropdownItem<Guid>>(sql, null, cancellationToken);
        return records;
    }

    public async Task<double> GetVehicleOdometerAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "LastOdo"
            FROM public."Vehicles"
            WHERE "Id" = @VehicleId
        """;
        double? odometer = await _dbQuery.SingleOrDefaultAsync<double?>(sql, new { VehicleId = vehicleId }, cancellationToken);
        return odometer ?? 0;
    }

    public static IEnumerable<DropdownItem<ActionType>> GetActionTypes()
    {
        return EnumExtensions.ToList<ActionType>()
            .Select(e => new DropdownItem<ActionType>((ActionType)e.Value, e.Code, e.Description));
    }

    public static IEnumerable<DropdownItem<AssignmentPlanType>> GetAssignmentPlanTypes()
    {
        return EnumExtensions.ToList<AssignmentPlanType>()
            .Select(e => new DropdownItem<AssignmentPlanType>((AssignmentPlanType)e.Value, e.Code, e.Description));
    }

    public async Task<LatestAssignmentResponse?> GetLatestAssignmentAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                a."Id" AS "AssignmentId",
                vd."Odometer",
                a."StartOfficeCode",
                vd."Address",
                vd."CheckStatus",
                a."PlanningId",
                a."PlanningType"
            FROM public."VehicleDrivers" vd
            INNER JOIN public."Assignments" a ON vd."AssignmentId" = a."Id"
            WHERE vd."VehicleId" = @VehicleId
                AND a."IsCompleted" = false
            ORDER BY vd."Code" DESC
            LIMIT 1
        """;
        return await _dbQuery.SingleOrDefaultAsync<LatestAssignmentResponse>(sql, new { VehicleId = vehicleId }, cancellationToken);
    }

    public async Task<IEnumerable<DropdownItem<Guid>>> GetInspectionPlansByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Code", "Name"
            FROM public."InspectionPlans"
            WHERE "VehicleId" = @VehicleId and "Status" = Any(@Statuses)
        """;
        IEnumerable<DropdownItem<Guid>> records = await _dbQuery.QueryAsync<DropdownItem<Guid>>(
            sql,
            new { VehicleId = vehicleId, Statuses = new[] { (int)InspectionPlanStatus.WaitingForInspection, (int)InspectionPlanStatus.InProgress } },
            cancellationToken);
        return records;
    }

    public async Task<IEnumerable<DropdownItem<Guid>>> GetMaintenancePlanByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Code", "Name"
            FROM public."MaintenancePlans"
            WHERE "VehicleId" = @VehicleId and "Status" = Any(@Statuses)
        """;
        IEnumerable<DropdownItem<Guid>> records = await _dbQuery.QueryAsync<DropdownItem<Guid>>(
            sql,
            new { VehicleId = vehicleId, Statuses = new[] { (int)MaintenancePlanStatus.Pending, (int)MaintenancePlanStatus.InTransit, (int)MaintenancePlanStatus.InProgress } },
            cancellationToken);
        return records;
    }
}
