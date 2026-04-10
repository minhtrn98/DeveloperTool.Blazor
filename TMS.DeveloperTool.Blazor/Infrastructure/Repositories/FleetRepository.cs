using TMS.DeveloperTool.Blazor.Domain.Enums;
using VehicleRecord = (System.Guid Id, string LicensePlate);

namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class FleetRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public FleetRepository([FromKeyedServices("FleetDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<Dictionary<Guid, string>> GetVehiclePlatesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "LicensePlate"
            FROM public."Vehicles"
            WHERE "Id" = ANY(@Ids)
        """;
        IEnumerable<VehicleRecord> vehicles = await _dbQuery.QueryAsync<VehicleRecord>(sql, new { Ids = ids }, cancellationToken);
        return vehicles.ToDictionary(d => d.Id, d => d.LicensePlate);
    }

    public async Task<List<VehicleDropdownItemDto>> GetVehicleDropdownItemsAsync(bool? isPairingAllowed, CancellationToken cancellationToken = default)
    {
        string sql = isPairingAllowed.HasValue ? """
            SELECT "Id", "Code", "LicensePlate", "DeptManagerCode"
            FROM public.mv_vehicles_with_status
            WHERE "IsPairingAllowed" = @IsPairingAllowed
            order by "DeptManagerCode" nulls last, "LicensePlate"
        """ : """
            SELECT "Id", "Code", "LicensePlate", "DeptManagerCode"
            FROM public.mv_vehicles_with_status
            order by "DeptManagerCode" nulls last, "LicensePlate"
        """;

        object? parameters = isPairingAllowed.HasValue ? new { IsPairingAllowed = isPairingAllowed } : null;
        IEnumerable<VehicleDropdownItemDto> vehicles = await _dbQuery.QueryAsync<VehicleDropdownItemDto>(sql, parameters, cancellationToken);

        return vehicles.ToList();
    }

    public async Task<(int totalCount, List<VehicleDropdownItemDto>)> SearchVehicleWithPaging(int page, int pageSize, string? searchValue, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Code", "LicensePlate", "DeptManagerCode"
            FROM public.mv_vehicles_with_status
            where (@SearchValue IS NULL OR "SearchValue" ILIKE @SearchValue)
            order by "LicensePlate"
            limit @PageSize offset @Offset
        """;

        const string sqlCount = """
            SELECT count(1)
            FROM public.mv_vehicles_with_status
            where (@SearchValue IS NULL OR "SearchValue" ILIKE @SearchValue)
        """;

        object parameters = new { PageSize = pageSize, Offset = (page - 1) * pageSize, SearchValue = $"%{searchValue}%" };
        IEnumerable<VehicleDropdownItemDto> vehicles = await _dbQuery.QueryAsync<VehicleDropdownItemDto>(sql, parameters, cancellationToken);
        int totalCount = await _dbQuery.SingleOrDefaultAsync<int>(sqlCount, new { SearchValue = $"%{searchValue}%" }, cancellationToken);

        return (totalCount, vehicles.ToList());
    }

    public async Task<IEnumerable<Guid>> GetAllVehicleIdsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id"
            FROM public."Vehicles"
        """;
        return await _dbQuery.QueryAsync<Guid>(sql, null, cancellationToken);
    }

    public async Task<IEnumerable<DropdownItemDto<Guid>>> GetPairingReasonTypesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select  "Id" ,"Code" ,"Name"
            from public."PairingReasonTypes"
            where "IsDeleted" = false
        """;
        IEnumerable<DropdownItemDto<Guid>> records = await _dbQuery.QueryAsync<DropdownItemDto<Guid>>(sql, null, cancellationToken);
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

    public async Task<double> GetVehicleOdometerAsync(string licensePlate, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "LastOdo"
            FROM public."Vehicles"
            WHERE "ActualPlate" = @LicensePlate or "LicensePlate" = @LicensePlate
        """;
        double? odometer = await _dbQuery.SingleOrDefaultAsync<double?>(sql, new { LicensePlate = licensePlate }, cancellationToken);
        return odometer ?? 0;
    }

    public static IEnumerable<DropdownItemDto<ActionType>> GetActionTypes()
    {
        return EnumExtensions.ToList<ActionType>()
            .Select(e => new DropdownItemDto<ActionType>((ActionType)e.Value, e.Code, e.Description));
    }

    public static IEnumerable<DropdownItemDto<AssignmentPlanType>> GetAssignmentPlanTypes()
    {
        return EnumExtensions.ToList<AssignmentPlanType>()
            .Select(e => new DropdownItemDto<AssignmentPlanType>((AssignmentPlanType)e.Value, e.Code, e.Description));
    }

    public async Task<LatestAssignmentDto?> GetLatestAssignmentAsync(Guid vehicleId, CancellationToken cancellationToken = default)
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
        return await _dbQuery.SingleOrDefaultAsync<LatestAssignmentDto>(sql, new { VehicleId = vehicleId }, cancellationToken);
    }

    public async Task<IEnumerable<PlanningDropdownItemDto>> GetInspectionPlansByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Code", "Name", "Status", "DeptManagerCode" as "DepartmentCode"
            FROM public."InspectionPlans"
            WHERE "VehicleId" = @VehicleId and "Status" = Any(@Statuses)
        """;
        IEnumerable<PlanningDropdownItemDto> records = await _dbQuery.QueryAsync<PlanningDropdownItemDto>(
            sql,
            new { VehicleId = vehicleId, Statuses = new[] { (int)InspectionPlanStatus.WaitingForInspection, (int)InspectionPlanStatus.InProgress } },
            cancellationToken);
        return records;
    }

    public async Task<IEnumerable<PlanningDropdownItemDto>> GetMaintenancePlansByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Code", "Name", "Status", "DeptManagerCode" as "DepartmentCode"
            FROM public."MaintenancePlans"
            WHERE "VehicleId" = @VehicleId and "Status" = Any(@Statuses)
        """;
        IEnumerable<PlanningDropdownItemDto> records = await _dbQuery.QueryAsync<PlanningDropdownItemDto>(
            sql,
            new { VehicleId = vehicleId, Statuses = new[] { (int)MaintenancePlanStatus.Pending, (int)MaintenancePlanStatus.InTransit, (int)MaintenancePlanStatus.InProgress } },
            cancellationToken);
        return records;
    }
}
