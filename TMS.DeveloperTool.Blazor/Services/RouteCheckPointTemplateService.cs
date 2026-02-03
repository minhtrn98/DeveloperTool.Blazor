using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Database;
using TMS.DeveloperTool.Blazor.Entities;

namespace TMS.DeveloperTool.Blazor.Services;

public class RouteCheckPointTemplateService
{
    private readonly ApplicationDbContext _context;

    public RouteCheckPointTemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RouteCheckPointTemplate>> GetAllAsync()
    {
        return await _context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints.OrderBy(cp => cp.Order))
            .ToListAsync();
    }

    public async Task<RouteCheckPointTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints.OrderBy(cp => cp.Order))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<RouteCheckPointTemplate> CreateAsync(RouteCheckPointTemplate template)
    {
        template.Id = Guid.NewGuid();
        foreach (var checkPoint in template.RouteCheckPoints)
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
        var existingTemplate = await _context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints)
            .FirstOrDefaultAsync(t => t.Id == template.Id);

        if (existingTemplate == null)
        {
            throw new InvalidOperationException("Template not found");
        }

        // Update template properties
        existingTemplate.JumpSeconds = template.JumpSeconds;

        // Remove old checkpoints
        _context.RouteCheckPoints.RemoveRange(existingTemplate.RouteCheckPoints);

        // Add new checkpoints
        foreach (var checkPoint in template.RouteCheckPoints)
        {
            checkPoint.Id = Guid.NewGuid();
            checkPoint.TemplateId = template.Id;
            existingTemplate.RouteCheckPoints.Add(checkPoint);
        }

        await _context.SaveChangesAsync();
        return existingTemplate;
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await _context.RouteCheckPointTemplates
            .Include(t => t.RouteCheckPoints)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template != null)
        {
            _context.RouteCheckPointTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}
