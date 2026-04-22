using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.ApiRequest.Services;

public sealed class ApiRequestHistoryService(ApplicationDbContext dbContext)
{
    public async Task SaveRequestAsync(
        string name,
        string method,
        string service,
        string endpoint,
        string jsonBody,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO request_histories (name, method, service, endpoint, json_body, created_at)
            VALUES ({name}, {method}, {service}, {endpoint}, {jsonBody}, {now})
            ON CONFLICT (name, method, service, endpoint, json_body)
            DO UPDATE SET created_at = EXCLUDED.created_at;
            """, cancellationToken);
    }

    public async Task<List<RequestHistory>> GetLatestAsync(int take, CancellationToken cancellationToken)
    {
        return await dbContext.RequestHistories
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByIdAsync(long id, CancellationToken cancellationToken)
    {
        RequestHistory? history = await dbContext.RequestHistories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (history is null)
        {
            return;
        }

        dbContext.RequestHistories.Remove(history);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken)
    {
        await dbContext.RequestHistories.ExecuteDeleteAsync(cancellationToken);
    }
}
