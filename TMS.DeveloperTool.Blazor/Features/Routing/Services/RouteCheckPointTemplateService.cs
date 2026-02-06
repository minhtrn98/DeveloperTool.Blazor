using Microsoft.EntityFrameworkCore;

namespace TMS.DeveloperTool.Blazor.Features.Routing.Services;

public sealed class RouteCheckPointTemplateService
{
    private readonly ApplicationDbContext _context;

    public RouteCheckPointTemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RouteCheckPointTemplate>> GetAllAsync()
    {
        return await _context.RouteCheckPointTemplates
            .AsNoTracking()
            .Include(t => t.RouteCheckPoints.OrderBy(cp => cp.Order))
            .ToListAsync();
    }

    public async Task<RouteCheckPointTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.RouteCheckPointTemplates
            .AsNoTracking()
            .Include(t => t.RouteCheckPoints.OrderBy(cp => cp.Order))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<RouteCheckPointTemplate> CreateAsync(RouteCheckPointTemplate template)
    {
        template.Id = Guid.NewGuid();
        foreach (RouteCheckPoint checkPoint in template.RouteCheckPoints)
        {
            checkPoint.Id = Guid.NewGuid();
            checkPoint.TemplateId = template.Id;
        }

        _context.RouteCheckPointTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<RouteCheckPointTemplate> UpdateAsync(RouteCheckPointTemplate template)
    {
        RouteCheckPointTemplate? existingTemplate = await _context.RouteCheckPointTemplates
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
        _context.RouteCheckPoints.RemoveRange(existingTemplate.RouteCheckPoints);

        // Add new checkpoints
        foreach (RouteCheckPoint checkPoint in template.RouteCheckPoints)
        {
            _context.RouteCheckPoints.Add(new RouteCheckPoint
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Order = checkPoint.Order,
                Lon = checkPoint.Lon,
                Lat = checkPoint.Lat,
                Address = checkPoint.Address,
                Km = checkPoint.Km,
            });
        }

        await _context.SaveChangesAsync();
        return existingTemplate;
    }

    public async Task DeleteAsync(Guid id)
    {
        RouteCheckPointTemplate? template = await _context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template != null)
        {
            _context.RouteCheckPointTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}
