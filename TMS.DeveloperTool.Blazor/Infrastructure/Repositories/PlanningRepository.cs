namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class PlanningRepository
{
    private readonly ApplicationDbQuery _dbQuery;

    public PlanningRepository([FromKeyedServices("PlanningDb")] ApplicationDbQuery dbQuery)
    {
        _dbQuery = dbQuery;
    }

    public async Task<IEnumerable<PlanningDropdownItemDto>> GetDailyPlanDropdownItemsAsync(CancellationToken cancellationToken)
    {
        // create gmt+7 now datetime
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset gmt7Now = utcNow.ToOffset(TimeSpan.FromHours(7));
        string todayGmt7 = gmt7Now.ToString("yyyy-MM-dd");

        string sql = $"""
            select id as "Id", code as "Code", name as "Name", status as "Status", department_code as "DepartmentCode"
            from public.real_plans
            where execution_date = '{todayGmt7}'
        """;
        IEnumerable<PlanningDropdownItemDto> records = await _dbQuery.QueryAsync<PlanningDropdownItemDto>(sql, null, cancellationToken);
        return records;
    }

    public async Task<IEnumerable<DailyPlanDto>> GetDailyPlansAsync(string? status, CancellationToken cancellationToken)
    {
        // create gmt+7 now datetime
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset gmt7Now = utcNow.ToOffset(TimeSpan.FromHours(7));
        string todayGmt7 = gmt7Now.ToString("yyyy-MM-dd");

        string sqlDailyPlan = $"""
            select id as "Id"
                , code as "Code"
                , name as "Name"
                , status as "Status"
                , department_code as "DepartmentCode"
                , execution_date as "ExecutionDate"
            from public.real_plans
            where execution_date = '{todayGmt7}' and (@Status IS NULL OR status = @Status)
        """;
        IEnumerable<DailyPlanDto> dailyPlans = await _dbQuery.QueryAsync<DailyPlanDto>(sqlDailyPlan, new { Status = status }, cancellationToken);
        Guid[] dailyPlanIds = [.. dailyPlans.Select(dp => dp.Id)];

        const string sqlDailyPlanDetails = $"""
            select d.id as "Id"
                , d.real_plan_id as "DailyPlanId"
                , d.post_office_code as "PostOfficeCode"
                , d.from_time as "FromTime"
                , d.to_time as "ToTime"
                , d.business_operation as "BusinessOperation"
                , d.step_number as "StepNumber"
            from public.real_plan_details d
            join UNNEST(@Ids) as r(id) on d.real_plan_id = r.id
        """;
        IEnumerable<DailyPlanDetailDto> dailyPlanDetails = await _dbQuery.QueryAsync<DailyPlanDetailDto>(sqlDailyPlanDetails, new { Ids = dailyPlanIds }, cancellationToken);
        foreach (DailyPlanDto dailyPlan in dailyPlans)
        {
            dailyPlan.Details = dailyPlanDetails.Where(dpd => dpd.DailyPlanId == dailyPlan.Id).OrderBy(x => x.StepNumber).ToList();
        }

        return dailyPlans;
    }
}