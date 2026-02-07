namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class PlanningRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public PlanningRepository([FromKeyedServices("PlanningDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<IEnumerable<DropdownItem<Guid>>> GetDailyPlans(CancellationToken cancellationToken)
    {
        const string sql = """
            select id as "Id", code as "Code", name as "Name"
            from public.real_plans
            where is_deleted = false
        """;
        IEnumerable<DropdownItem<Guid>> records = await _dbQuery.QueryAsync<DropdownItem<Guid>>(sql, null, cancellationToken);
        return records;
    }
}