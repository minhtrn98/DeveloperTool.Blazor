using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyEmployeeService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Employees.AsNoTracking().ToListAsync(cancellationToken);
    }
}