namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class PlanningRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public PlanningRepository([FromKeyedServices("PlanningDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<IEnumerable<DropdownItemPlanning>> GetDailyPlans(CancellationToken cancellationToken)
    {
        // create gmt+7 now datetime
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset gmt7Now = utcNow.ToOffset(TimeSpan.FromHours(7));
        string todayGmt7 = gmt7Now.ToString("yyyy-MM-dd");

        string sql = $"""
            select id as "Id", code as "Code", name as "Name", status as "Status"
            from public.real_plans
            where execution_date = '{todayGmt7}'
        """;
        IEnumerable<DropdownItemPlanning> records = await _dbQuery.QueryAsync<DropdownItemPlanning>(sql, null, cancellationToken);
        return records;
    }
}