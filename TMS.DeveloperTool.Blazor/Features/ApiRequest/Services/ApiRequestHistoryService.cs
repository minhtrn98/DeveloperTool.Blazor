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
        RequestHistory item = new()
        {
            Name = name,
            Method = method,
            Service = service,
            Endpoint = endpoint,
            JsonBody = jsonBody,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.RequestHistories.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
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
