using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.Routing.Services;

public sealed class RouteCheckPointTemplateService(ApplicationDbContext context)
{
    public async Task<List<RouteCheckPointTemplate>> GetAllAsync()
    {
        return await context.RouteCheckPointTemplates
            .AsNoTracking()
            .Include(t => t.RouteCheckPoints.OrderBy(cp => cp.Order))
            .ToListAsync();
    }

    public async Task<RouteCheckPointTemplate?> GetByIdAsync(Guid id)
    {
        return await context.RouteCheckPointTemplates
            .AsNoTracking()
            .Include(t => t.RouteCheckPoints.OrderBy(cp => cp.Order))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<RouteCheckPointTemplate> CreateAsync(RouteCheckPointTemplate template)
    {
        template.Id = Guid.CreateVersion7();
        foreach (RouteCheckPoint checkPoint in template.RouteCheckPoints)
        {
            checkPoint.Id = Guid.CreateVersion7();
            checkPoint.TemplateId = template.Id;
        }

        context.RouteCheckPointTemplates.Add(template);
        await context.SaveChangesAsync();
        return template;
    }

    public async Task<RouteCheckPointTemplate> UpdateAsync(RouteCheckPointTemplate template)
    {
        RouteCheckPointTemplate? existingTemplate = await context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints)
            .FirstOrDefaultAsync(t => t.Id == template.Id);

        if (existingTemplate == null)
        {
            throw new InvalidOperationException("Template not found");
        }

        // Update template properties
        existingTemplate.JumpSeconds = template.JumpSeconds;
        existingTemplate.Name = template.Name;

        // Clear existing checkpoints
        context.RouteCheckPoints.RemoveRange(existingTemplate.RouteCheckPoints);

        // Add new checkpoints
        foreach (RouteCheckPoint checkPoint in template.RouteCheckPoints)
        {
            context.RouteCheckPoints.Add(new RouteCheckPoint
            {
                Id = Guid.CreateVersion7(),
                TemplateId = template.Id,
                Order = checkPoint.Order,
                Lon = checkPoint.Lon,
                Lat = checkPoint.Lat,
                Address = checkPoint.Address,
                Km = checkPoint.Km,
            });
        }

        await context.SaveChangesAsync();
        return existingTemplate;
    }

    public async Task DeleteAsync(Guid id)
    {
        RouteCheckPointTemplate? template = await context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template != null)
        {
            context.RouteCheckPointTemplates.Remove(template);
            await context.SaveChangesAsync();
        }
    }
}
